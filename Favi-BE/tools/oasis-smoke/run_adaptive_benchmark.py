"""Frozen adaptive-v1 common-candidate selection and refit; no legacy tuning."""
import os
for variable in ('OPENBLAS_NUM_THREADS', 'MKL_NUM_THREADS', 'OMP_NUM_THREADS'):
    os.environ[variable] = '4'

import argparse
import hashlib
import itertools
import json
import math
from pathlib import Path
import statistics
import time
from adaptive_feed import AdaptiveFeed
from analyze_adaptive_cohorts import analyze, COHORTS
from benchmark_recommenders import evaluate, summarize
from collaborative_models import InteractionData, fit_ease, fit_ppr, fit_lightgcn
from dataset_policy import fingerprint
from source_provenance import verify_dependency_lock


def save(path, value):
    path.write_text(json.dumps(value, indent=2, allow_nan=False) + '\n', encoding='utf-8')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def select(records, families=('ease', 'ppr', 'lightgcn'), threshold=None, tolerance=1e-12):
    eligible = [r for r in records if r['family'] in families and
                (threshold is None or r['threshold'] == threshold)]
    if not eligible or any(not math.isfinite(r['ndcg']) for r in eligible):
        raise ValueError('Require finite validation scores')
    maximum = max(r['ndcg'] for r in eligible)
    tied = [r for r in eligible if maximum - r['ndcg'] <= tolerance]
    return min(tied, key=lambda r: (r['threshold'], families.index(r['family']),
                                    r['grid_index'], r.get('epoch', 0)))


class DualPatience:
    """Track strict nDCG improvements independently; keep evaluating both routes."""
    def __init__(self, thresholds, patience):
        self.best = {t: -math.inf for t in thresholds}
        self.stale = dict.fromkeys(thresholds, 0)
        self.patience = patience

    def update(self, scores):
        if set(scores) != set(self.best) or any(not math.isfinite(v) for v in scores.values()):
            raise ValueError('Missing/nonfinite threshold checkpoint')
        for threshold, score in scores.items():
            if score > self.best[threshold]:
                self.best[threshold], self.stale[threshold] = score, 0
            else:
                self.stale[threshold] += 1
        return not all(v >= self.patience for v in self.stale.values())


def aggregate_optimizer_datasets(datasets):
    """Average optimizer repeats inside each dataset before dataset statistics."""
    if not datasets or any(not values for values in datasets.values()):
        raise ValueError('Empty dataset/optimizer group')
    means = {key: statistics.mean(values) for key, values in datasets.items()}
    return dict(dataset_means=means, mean=statistics.mean(means.values()),
                dataset_std=statistics.stdev(means.values()) if len(means) > 1 else 0,
                optimizer_std={key: statistics.stdev(values) if len(values) > 1 else 0
                               for key, values in datasets.items()})


def tune(prefix, onboarding, config, score, progress=lambda records: None):
    first, second, _ = config['cutoffs']
    if any(e['tick'] >= second for e in prefix):
        raise ValueError('Selector must receive validation prefix only')
    train = InteractionData(prefix, first)
    if len(train.items) > 5000:
        raise ValueError('Dense collaborative catalog exceeds 5000')
    records = []

    def assess(model, family, index, parameters, epoch=0):
        scores = {}
        for threshold in config['thresholds']:
            feed = AdaptiveFeed(prefix, first, onboarding, method='collaborative',
                                warm_model=model, threshold=threshold)
            result = score(feed)
            record = dict(family=family, grid_index=index, parameters=parameters,
                          epoch=epoch, threshold=threshold, ndcg=result['all']['ndcg'],
                          population_sha256=result['evaluation_population_sha256'])
            records.append(record)
            scores[threshold] = record['ndcg']
        progress(records)
        return scores

    for family, values, fit in (('ease', config['ease_regularization'], fit_ease),
                                ('ppr', config['ppr_alpha'], fit_ppr)):
        for index, value in enumerate(values):
            assess(fit(train, value), family, index, {'value': value})
    for index, (layers, rate) in enumerate(itertools.product(config['lightgcn_layers'],
                                                             config['lightgcn_learning_rates'])):
        parameters = dict(dimensions=config['lightgcn_dimensions'], layers=layers,
                          learning_rate=rate, regularization=config['lightgcn_regularization'],
                          epochs=config['lightgcn_max_epochs'], check_every=config['lightgcn_check_every'])
        patience = DualPatience(config['thresholds'], config['lightgcn_patience'])

        def checkpoint(epoch, model, record):
            # Legacy trainer also snapshots epoch1. Protocol allows only every10.
            if epoch % config['lightgcn_check_every']:
                return True
            return patience.update(assess(model, 'lightgcn', index,
                                         {**parameters, 'epochs': epoch}, epoch))

        fit_lightgcn(train, parameters, config['training_seed'], checkpoint=checkpoint)
    v2 = config['formula_v2']
    for index, (social, decay, cap) in enumerate(itertools.product(v2['social_weight'], v2['decay'], v2['author_cap'])):
        parameters = dict(social_weight=social, decay=decay, author_cap=cap)
        result = score(AdaptiveFeed(prefix, first, onboarding, method='formula-v2', parameters=parameters))
        records.append(dict(family='formula-v2', grid_index=index, parameters=parameters,
                            epoch=0, threshold=1, ndcg=result['all']['ndcg'],
                            population_sha256=result['evaluation_population_sha256']))
        progress(records)
    if len({r['population_sha256'] for r in records}) != 1:
        raise ValueError('Tuning populations differ')
    tolerance = config['selection_tolerance']
    choices = {'B1': select(records, threshold=1, tolerance=tolerance),
               'R': select(records, tolerance=tolerance),
               'formula-v2': select(records, ('formula-v2',), tolerance=tolerance)}
    for family in config['method_order']:
        choices[family] = select(records, (family,), threshold=1, tolerance=tolerance)
    return choices, records


def refit(data, choice, seed):
    family, parameters = choice['family'], choice['parameters']
    if family == 'ease':
        return fit_ease(data, parameters['value'])
    if family == 'ppr':
        return fit_ppr(data, parameters['value'])
    if family == 'lightgcn':
        # No callback: exactly the validation-selected epoch, never test stopping.
        return fit_lightgcn(data, parameters, seed)[0]
    raise ValueError('Unsupported collaborative family')


SOURCE_NAMES = ('run_adaptive_benchmark.py', 'adaptive_feed.py', 'adaptive_onboarding.py',
                'favi_rankers.py', 'analyze_adaptive_cohorts.py', 'benchmark_recommenders.py',
                'collaborative_models.py', 'check_dataset.py', 'dataset_policy.py',
                'source_provenance.py', 'requirements-oasis.lock.txt',
                'test_adaptive_benchmark.py', 'test_adaptive_feed.py', 'community_policy.py')


def run(dataset, output, config_path):
    dataset, output = dataset.resolve(), output.resolve()
    if output.exists() or output.is_relative_to(dataset):
        raise ValueError('Use fresh output outside dataset')
    config = json.loads(config_path.read_text(encoding='utf-8-sig'))
    frozen = Path(__file__).with_name('configs') / 'recommenders' / 'adaptive-v1.json'
    if config != json.loads(frozen.read_text(encoding='utf-8-sig')):
        raise ValueError('Unsupported adaptive-v1 configuration revision')
    gate = analyze(dataset)
    if not gate['gate_passed']:
        raise ValueError('Scenario data gate failed')
    locked = verify_dependency_lock(Path(__file__).with_name('requirements-oasis.lock.txt'))
    resolved = json.loads((dataset / 'config.resolved.json').read_text(encoding='utf-8-sig'))
    if resolved['ticks'] != config['cutoffs'][2]:
        raise ValueError('Dataset tick contract mismatch')
    events = [json.loads(line) for line in (dataset / 'events.jsonl').read_text().splitlines()]
    onboarding = json.loads((dataset / 'onboarding.json').read_text())
    first, second, end = config['cutoffs']
    prefix = [e for e in events if e['tick'] < second]
    output.mkdir(parents=True)
    save(output / 'protocol.json', config)
    (output / 'config.original.json').write_bytes(config_path.read_bytes())
    protocol_doc = Path(__file__).resolve().parents[2] / 'document' / 'benchmarks' / 'adaptive-protocol-v1.md'
    (output / 'adaptive-protocol-v1.md').write_bytes(protocol_doc.read_bytes())
    for name in SOURCE_NAMES:
        (output / name).write_bytes(Path(__file__).with_name(name).read_bytes())
    inventory = {name: sha(output / name) for name in SOURCE_NAMES}
    save(output / 'source-inventory.json', inventory)
    protocol = dict(k=config['k'], hours_per_tick=config['hours_per_tick'], timing_repeats=1)
    populations = {}

    def assess(feed, phase, label, seed=None, persist=False):
        start, stop = (first, second) if phase == 'validation' else (second, end)
        result, rows = evaluate(prefix if phase == 'validation' else events,
                                start, stop, label, protocol, fitted_model=feed, measure=False)
        population = result['evaluation_population_sha256']
        if populations.setdefault(phase, population) != population:
            raise ValueError('Common candidate/target/user population differs')
        for row in rows:
            row['adaptive_cohort'] = feed.cohort(row['user_id'])
        result['adaptive_cohorts'] = {c: summarize([r for r in rows if r['adaptive_cohort'] == c], len(feed.items)) for c in COHORTS}
        for c in gate['required_cohorts']:
            if result['adaptive_cohorts'][c]['users'] != gate['phases'][phase]['cohorts'][c]['with_target']:
                raise ValueError('Gate/evaluator denominator differs')
        result.update(phase=phase, label=label, optimizer_seed=seed)
        if persist:
            result['feed_diagnostics'] = dict(
                scope='target users only; full no-target denominators in gate',
                empty_users=sum(not r['recommended'] for r in rows),
                short_users=sum(len(r['recommended']) < config['k'] for r in rows),
                mean_item_age_hours=statistics.mean([
                    (start - feed.items[item]['tick']) * config['hours_per_tick']
                    for row in rows for item in row['recommended']])
                    if any(row['recommended'] for row in rows) else None)
            cold_rows = [r for r in rows if feed.method == 'topic-only' or
                         (feed.method != 'popularity' and len(feed.seen.get(r['user_id'], ())) < feed.threshold)]
            result['feed_diagnostics']['cold_target_pool_misses'] = sum(
                len(set(row['relevant']) - set(feed.cold(row['user_id'], len(feed.items)))) for row in cold_rows)
            suffix = '' if seed is None else '-' + str(seed)
            (output / f'{phase}-{label}{suffix}-users.jsonl').write_text(
                ''.join(json.dumps(row) + '\n' for row in rows), encoding='utf-8')
        return result

    def progress(records):
        save(output / 'tuning.json', records)
        print(json.dumps(dict(trials=len(records), latest=records[-1])), flush=True)

    choices, trials = tune(prefix, onboarding, config, lambda feed: assess(feed, 'validation', 'trial'), progress)
    save(output / 'selection.json', dict(choices=choices, prefix_sha256=fingerprint(prefix),
         config_sha256=sha(output / 'protocol.json'), source_inventory=inventory,
         onboarding_sha256=sha(dataset / 'onboarding.json'), test_not_used=True))
    selection_hash = sha(output / 'selection.json')
    # Re-read the persisted decision before creating any test evaluator.
    choices = json.loads((output / 'selection.json').read_text())['choices']
    data = InteractionData(prefix, second)
    if len(data.items) > 5000:
        raise ValueError('Refit catalog exceeds 5000')
    results, cache = [], {}
    for label, choice in choices.items():
        if label == 'formula-v2':
            feed = AdaptiveFeed(prefix, second, onboarding, method='formula-v2', parameters=choice['parameters'])
            results.append(assess(feed, 'test', label, persist=True))
            continue
        seeds = config['test_training_seeds'] if choice['family'] == 'lightgcn' else [None]
        for seed in seeds:
            key = (choice['family'], json.dumps(choice['parameters'], sort_keys=True), seed)
            if key not in cache:
                begun = time.perf_counter()
                cache[key] = (refit(data, choice, seed), time.perf_counter() - begun)
            model, seconds = cache[key]
            feed = AdaptiveFeed(prefix, second, onboarding, method='collaborative', warm_model=model, threshold=choice['threshold'])
            result = assess(feed, 'test', label, seed, persist=True)
            result.update(refit_seconds=seconds, model_state_bytes=model.state_bytes())
            results.append(result)
    for method in ('popularity', 'topic-only', 'formula'):
        results.append(assess(AdaptiveFeed(prefix, second, onboarding, method=method), 'test', method, persist=True))
    if sha(output / 'selection.json') != selection_hash:
        raise ValueError('Frozen selection mutated')
    save(output / 'report.json', dict(protocol_id=config['protocol_id'], gate=gate, locked_dependencies=locked,
         selection_sha256=selection_hash, results=results,
         limitations=['Synthetic replay; no SQL/HTTP parity or causal effect',
                      'Validation optimizer seed only20261103; optimizer repeats are not dataset repeats',
                      'Main primary and sparse scenario must be aggregated separately']))
    manifest = {p.name: sha(p) for p in output.iterdir() if p.is_file()}
    save(output / 'manifest.json', manifest)
    for name, expected in json.loads((output / 'manifest.json').read_text()).items():
        if sha(output / name) != expected:
            raise ValueError('Artifact readback mismatch')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dataset', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--config', type=Path, default=Path(__file__).with_name('configs') / 'recommenders' / 'adaptive-v1.json')
    args = parser.parse_args()
    from threadpoolctl import threadpool_limits
    with threadpool_limits(limits=4):
        run(args.dataset, args.output, args.config)

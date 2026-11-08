"""Secondary topic robustness/ablations with immutable informative selection."""
import os
for variable in ('OPENBLAS_NUM_THREADS', 'MKL_NUM_THREADS', 'OMP_NUM_THREADS'):
    os.environ[variable] = '4'

import argparse
from collections import Counter
import json
from pathlib import Path
import time
from adaptive_feed import AdaptiveFeed
from adaptive_onboarding import validate
from analyze_adaptive_cohorts import analyze, COHORTS
from benchmark_recommenders import evaluate, summarize
from collaborative_models import InteractionData
from dataset_policy import fingerprint
from run_adaptive_benchmark import refit, save, sha, SOURCE_NAMES
from source_provenance import verify_dependency_lock


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def verify_manifest(root):
    manifest = read(root / 'manifest.json')
    for name, expected in manifest.items():
        path = root / name
        if path.parent != root or sha(path) != expected:
            raise ValueError('Ranking manifest mismatch: ' + name)
    return sha(root / 'manifest.json')


def variants(primary, sidecars):
    """Preserve arrivals, preference times, IDs and ordering in every variant."""
    result = {'primary': primary, **sidecars,
              'no-topics': [{**row, 'topics': []} for row in primary]}
    identity = lambda rows: [{k: v for k, v in row.items() if k != 'topics'} for row in rows]
    for rows in result.values():
        if identity(rows) != identity(primary):
            raise ValueError('Sidecar changes population or timeline')
    return result


def tasks(choices, mode):
    for label, choice in choices.items():
        if mode == 'no-topics' and label not in ('B1', 'R'):
            continue
        yield label, choice, choice['threshold']
    if mode == 'primary':
        yield 'R-no-routing', choices['R'], 1


def metadata(primary_feed, feed, users):
    transitions = Counter()
    slots = overlap = changed = 0
    for user in users:
        before = primary_feed.onboarding[user]['topics']
        after = feed.onboarding[user]['topics']
        slots += len(after)
        overlap += len(set(before) & set(after))
        changed += before != after
        transitions[primary_feed.cohort(user) + '->' + feed.cohort(user)] += 1
    return dict(paired_users=len(users), changed_topic_users=changed,
                declared_slots=slots, overlap_with_primary=overlap,
                overlap_rate=overlap / slots if slots else None,
                cohort_transitions=dict(sorted(transitions.items())))


def paired_check(rows, original, prediction_equal=False):
    keys = ('user_id', 'candidate_sha256', 'relevant')
    if [{k: r[k] for k in keys} for r in rows] != [{k: r[k] for k in keys} for r in original]:
        raise ValueError('Paired user/candidate/target mismatch')
    if prediction_equal and any(r['recommended'] != o['recommended'] for r, o in zip(rows, original)):
        raise ValueError('Primary prediction replay differs')


def run(dataset, ranking, output):
    dataset, ranking, output = dataset.resolve(), ranking.resolve(), output.resolve()
    if output.exists() or output.is_relative_to(dataset) or output.is_relative_to(ranking):
        raise ValueError('Require fresh output outside input evidence')
    ranking_manifest = verify_manifest(ranking)
    config = read(ranking / 'protocol.json')
    frozen = Path(__file__).with_name('configs') / 'recommenders' / 'adaptive-v1.json'
    if config != read(frozen):
        raise ValueError('Frozen protocol mismatch')
    # No changed model/feed/evaluation implementation may silently replay a holdout.
    for name in SOURCE_NAMES:
        if sha(Path(__file__).with_name(name)) != read(ranking / 'source-inventory.json')[name]:
            raise ValueError('Original source drift: ' + name)
    gate = analyze(dataset)
    if not gate['gate_passed']:
        raise ValueError('Original dataset gate failed')
    lock = verify_dependency_lock(Path(__file__).with_name('requirements-oasis.lock.txt'))
    selection = read(ranking / 'selection.json')
    selection_hash = sha(ranking / 'selection.json')
    if selection['config_sha256'] != sha(ranking / 'protocol.json'):
        raise ValueError('Selection protocol mismatch')
    original_report = read(ranking / 'report.json')
    if original_report['selection_sha256'] != selection_hash:
        raise ValueError('Selection/report mismatch')
    events = [json.loads(line) for line in (dataset / 'events.jsonl').read_text().splitlines()]
    _, cutoff, end = config['cutoffs']
    prefix = [e for e in events if e['tick'] < cutoff]
    primary = read(dataset / 'onboarding.json')
    if fingerprint(prefix) != selection['prefix_sha256'] or sha(dataset / 'onboarding.json') != selection['onboarding_sha256']:
        raise ValueError('Selection dataset mismatch')
    sidecars = {mode: read(dataset / f'onboarding.{mode}.json') for mode in config['robustness_preferences']}
    all_variants = variants(primary, sidecars)
    resolved = read(dataset / 'config.resolved.json')
    for rows in all_variants.values():
        validate(rows, resolved['users'], resolved['ticks'])
    output.mkdir(parents=True)
    inputs = {'selection_sha256': selection_hash, 'ranking_manifest_sha256': ranking_manifest,
              'events_sha256': sha(dataset / 'events.jsonl'),
              'sidecar_fingerprints': {mode: fingerprint(rows) for mode, rows in all_variants.items()}}
    save(output / 'inputs.json', inputs)
    for name in (*SOURCE_NAMES, 'run_adaptive_replay.py', 'test_adaptive_replay.py'):
        (output / name).write_bytes(Path(__file__).with_name(name).read_bytes())
    save(output / 'source-inventory.json', {p.name: sha(p) for p in output.iterdir() if p.suffix == '.py'})
    data = InteractionData(prefix, cutoff)
    primary_feed = AdaptiveFeed(prefix, cutoff, primary)
    protocol = dict(k=config['k'], hours_per_tick=config['hours_per_tick'], timing_repeats=1)
    cache, results, mode_metadata = {}, [], {}
    expected_population = original_report['results'][0]['evaluation_population_sha256']
    for mode, onboarding in all_variants.items():
        for label, choice, threshold in tasks(selection['choices'], mode):
            seeds = config['test_training_seeds'] if choice['family'] == 'lightgcn' else [None]
            for seed in seeds:
                kwargs = dict(method='formula-v2', parameters=choice['parameters'])
                seconds = None
                if label != 'formula-v2':
                    key = (choice['family'], json.dumps(choice['parameters'], sort_keys=True), seed)
                    if key not in cache:
                        begun = time.perf_counter()
                        cache[key] = (refit(data, choice, seed), time.perf_counter() - begun)
                    model, seconds = cache[key]
                    kwargs = dict(method='collaborative', warm_model=model, threshold=threshold)
                feed = AdaptiveFeed(prefix, cutoff, onboarding, **kwargs)
                result, rows = evaluate(events, cutoff, end, label, protocol, fitted_model=feed, measure=False)
                if result['evaluation_population_sha256'] != expected_population:
                    raise ValueError('Original population differs')
                source_label = 'R' if label == 'R-no-routing' else label
                suffix = '' if seed is None else '-' + str(seed)
                original = [json.loads(line) for line in (ranking / f'test-{source_label}{suffix}-users.jsonl').read_text().splitlines()]
                paired_check(rows, original, mode == 'primary' and label != 'R-no-routing')
                for row in rows:
                    row['primary_cohort'] = primary_feed.cohort(row['user_id'])
                    row['adaptive_cohort'] = feed.cohort(row['user_id'])
                for field, key in (('adaptive_cohorts', 'adaptive_cohort'), ('primary_cohorts', 'primary_cohort')):
                    result[field] = {c: summarize([r for r in rows if r[key] == c], len(feed.items)) for c in COHORTS}
                if mode not in mode_metadata:
                    mode_metadata[mode] = metadata(primary_feed, feed, [r['user_id'] for r in rows])
                result.update(mode=mode, label=label, optimizer_seed=seed, threshold=threshold,
                              choice=choice, refit_seconds=seconds)
                path = output / f'{mode}-{label}{suffix}-users.jsonl'
                path.write_text(''.join(json.dumps(r) + '\n' for r in rows), encoding='utf-8')
                results.append(result)
                save(output / 'progress.json', dict(completed=len(results), mode=mode, label=label, seed=seed))
                print(json.dumps(dict(mode=mode, label=label, seed=seed, ndcg=result['all']['ndcg'])), flush=True)
        if mode in ('primary', 'informative', 'noisy', 'independent'):
            # Non-selected reference systems have no optimizer state to refit.
            for method in ('popularity', 'topic-only', 'formula'):
                feed = AdaptiveFeed(prefix, cutoff, onboarding, method=method)
                result, rows = evaluate(events, cutoff, end, method, protocol, fitted_model=feed, measure=False)
                suffix = ranking / f'test-{method}-users.jsonl'
                original = [json.loads(line) for line in suffix.read_text().splitlines()]
                paired_check(rows, original, mode == 'primary')
                if result['evaluation_population_sha256'] != expected_population:
                    raise ValueError('Reference population differs')
                for row in rows:
                    row['primary_cohort'] = primary_feed.cohort(row['user_id'])
                    row['adaptive_cohort'] = feed.cohort(row['user_id'])
                for field, key in (('adaptive_cohorts', 'adaptive_cohort'), ('primary_cohorts', 'primary_cohort')):
                    result[field] = {c: summarize([r for r in rows if r[key] == c], len(feed.items)) for c in COHORTS}
                (output / f'{mode}-{method}-users.jsonl').write_text(''.join(json.dumps(r) + '\n' for r in rows), encoding='utf-8')
                result.update(mode=mode, label=method, optimizer_seed=None)
                results.append(result)
    if sha(ranking / 'selection.json') != selection_hash or verify_manifest(ranking) != ranking_manifest:
        raise ValueError('Original evidence mutated')
    save(output / 'report.json', dict(protocol_id=config['protocol_id'], gate=gate,
         inputs=inputs, locked_dependencies=lock, results=results, metadata=mode_metadata,
         limitations=['Secondary fixed-selection replay, no retuning or causal effect',
                      'Sparse primary uses music scarcity; informative sidecar is pre-scarcity metadata',
                      'No-topics keeps cold general fallback; no-routing fixes R threshold1, retaining R parameters/epoch',
                      'Original and sidecar cohort denominators both reported; population remains paired']))
    save(output / 'manifest.json', {p.name: sha(p) for p in output.iterdir() if p.is_file()})
    verify_manifest(output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('dataset', 'ranking', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    from threadpoolctl import threadpool_limits
    with threadpool_limits(limits=4):
        run(args.dataset, args.ranking, args.output)

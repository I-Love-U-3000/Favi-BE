"""Verify fixed-scale runs and aggregate optimizer seeds within dataset seeds."""
import argparse
import hashlib
import itertools
import json
from pathlib import Path
import statistics

from check_dataset import verify

METHODS = ('popularity', 'ease', 'ppr', 'lightgcn')
METRICS = ('recall', 'ndcg', 'catalog_coverage', 'author_diversity', 'max_author_share')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def verify_manifest(root):
    root = root.resolve()
    manifest = read(root / 'manifest.json')
    require(isinstance(manifest, dict) and bool(manifest), 'Empty/invalid benchmark manifest')
    for name, expected in manifest.items():
        target = (root / name).resolve()
        require(target.is_relative_to(root) and target.is_file(), 'Manifest escapes root or missing file')
        require(sha(target) == expected, 'Artifact fingerprint mismatch: ' + name)
    return sha(root / 'manifest.json')


def describe(values):
    require(bool(values), 'No scores to aggregate')
    return {'mean': statistics.mean(values), 'std': statistics.stdev(values) if len(values) > 1 else 0,
            'min': min(values), 'max': max(values), 'n': len(values)}


def dataset_scores(results, training_seeds):
    test = [r for r in results if r['phase'] == 'test']
    require(set(r['method'] for r in test) == set(METHODS), 'Missing/unexpected test method')
    require(len(set(training_seeds)) == len(training_seeds), 'Duplicate expected optimizer seed')
    for phase in ('validation', 'test'):
        phase_rows = [r for r in results if r['phase'] == phase]
        require(bool(phase_rows) and len({r['evaluation_population_sha256'] for r in phase_rows}) == 1,
                'Different evaluation populations in ' + phase)
    scores = {}
    for method in METHODS:
        rows = [r for r in test if r['method'] == method]
        if method == 'lightgcn':
            require(len(rows) == len(training_seeds) and {r['seed'] for r in rows} == set(training_seeds),
                    'LightGCN optimizer seeds differ from declared plan')
        else:
            require(len(rows) == 1, 'Duplicate baseline result')
        scores[method] = {metric: describe([r['all'][metric] for r in rows]) for metric in METRICS}
    return scores


def aggregate(datasets):
    require(len(datasets) >= 2, 'Require multiple independent dataset seeds')
    require(len({d['seed'] for d in datasets}) == len(datasets), 'Duplicate dataset seed')
    # Each dataset receives one vote, after averaging its optimizer runs.
    return {method: {metric: describe([d['scores'][method][metric]['mean'] for d in datasets])
                     for metric in METRICS} for method in METHODS}


def paired_differences(datasets):
    pairs = []
    for left, right in itertools.combinations(METHODS, 2):
        metrics = {}
        for metric in ('recall', 'ndcg'):
            values = [d['scores'][left][metric]['mean'] - d['scores'][right][metric]['mean'] for d in datasets]
            metrics[metric] = {**describe(values), 'by_dataset_seed': dict(zip((str(d['seed']) for d in datasets), values)),
                               'wins': sum(v > 0 for v in values), 'ties': sum(v == 0 for v in values),
                               'losses': sum(v < 0 for v in values)}
        pairs.append({'left': left, 'right': right, 'left_minus_right': metrics})
    return pairs


def run(config_path, output):
    config = read(config_path)
    require(config['schema_version'] == 1, 'Unsupported experiment version')
    entries = config['primary_datasets']
    require(3 <= len(entries) <= 5 and len({e['seed'] for e in entries}) == len(entries), 'Declare 3..5 distinct dataset seeds')
    require(config['reference']['seed'] not in {e['seed'] for e in entries}, 'Reference cannot be a primary dataset')
    generation, protocol, search = (read(Path(config[k])) for k in ('generation_config', 'protocol', 'search_config'))
    expected_generation = {k: v for k, v in generation.items() if k != 'seed'}
    output = output.resolve()
    require(not output.exists(), 'Choose a fresh summary output')
    for entry in entries + [config['reference']]:
        require(not output.is_relative_to(Path(entry['dataset']).resolve()) and
                not output.is_relative_to(Path(entry['benchmark']).resolve()), 'Summary must be outside its inputs')
    loaded, harness_hashes, model_sources = [], [], []
    for index, entry in enumerate(entries + [config['reference']]):
        dataset, benchmark = Path(entry['dataset']), Path(entry['benchmark'])
        integrity = verify(dataset)
        resolved = read(dataset / 'config.resolved.json')
        require(resolved['seed'] == entry['seed'] and {k: v for k, v in resolved.items() if k != 'seed'} == expected_generation,
                'Generation config changed beyond seed')
        manifest_hash = verify_manifest(benchmark)
        report = read(benchmark / 'report.json')
        require(report['events_sha256'] == integrity['events_sha256'], 'Benchmark refers to different dataset')
        require(report['protocol'] == protocol and report['search'] == search, 'Protocol/search budget changed')
        require(sha(benchmark / 'selection.json') == report['selection_sha256'], 'Frozen selection changed')
        selection = read(benchmark / 'selection.json')
        require(selection['test_not_used_for_selection'] is True and selection['selections'] == report['selections'],
                'Selection provenance mismatch')
        raw = read(dataset / 'dataset-report.json')
        archive = Path(entry['archive'])
        receipt = read(Path(str(archive) + '.json'))
        require(receipt['events_sha256'] == integrity['events_sha256'] and receipt['sha256'] == sha(archive),
                'Archive receipt or checksum mismatch')
        scores = dataset_scores(report['results'], search['test_training_seeds'])
        for metric in ('recall', 'ndcg', 'catalog_coverage', 'author_diversity'):
            require(scores['lightgcn'][metric]['mean'] == report['lightgcn_seed_summary'][metric]['mean'] and
                    scores['lightgcn'][metric]['std'] == report['lightgcn_seed_summary'][metric]['std'],
                    'Reported optimizer mean/std differs from individual runs')
        if index < len(entries):
            harness_hashes.append(raw['harness_sha256'])
        model_sources.append({name: sha(benchmark / name) for name in ('collaborative_models.py', 'benchmark_recommenders.py', 'run_expanded_benchmark.py')})
        loaded.append({**entry, 'counts': raw['counts'], 'generation_quality': raw['quality'],
                       'events_sha256': integrity['events_sha256'], 'sqlite_sha256': raw['source_sha256'],
                       'benchmark_manifest_sha256': manifest_hash, 'selection_sha256': report['selection_sha256'],
                       'archive_sha256': receipt['sha256'], 'archive_bytes': receipt['bytes'],
                       'generation_harness_sha256': raw['harness_sha256'], 'model_source_sha256': model_sources[-1],
                       'scores': scores, 'selections': report['selections'],
                       'validation_results': [r for r in report['results'] if r['phase'] == 'validation'],
                       'test_results': [r for r in report['results'] if r['phase'] == 'test']})
    require(all(h == harness_hashes[0] for h in harness_hashes), 'Fresh datasets use different generation harnesses')
    require(all(h == model_sources[0] for h in model_sources), 'Models/evaluator versions differ')
    primary, reference = loaded[:-1], loaded[-1]
    summary, differences = aggregate(primary), paired_differences(primary)
    result = {'schema_version': 1, 'config_sha256': sha(config_path), 'protocol': protocol, 'search': search,
              'primary_datasets': primary, 'historical_reference': reference, 'dataset_seed_summary': summary,
              'paired_dataset_differences': differences,
              'verification': 'Raw manifests/quality, same config except seed, archive checksums, benchmark manifests, frozen selection, shared populations and model/harness versions passed',
              'limitations': ['Three fresh synthetic datasets, same behavior model, not real-user external validation',
                              'Optimizer seeds averaged within each dataset before equal-weight dataset aggregation',
                              'Historical reference excluded from primary means; no seed cherry-picking',
                              'Descriptive sample std and paired signs, not statistical significance',
                              'Full-batch CPU LightGCN and finite validation search, not exhaustive model comparison']}
    output.mkdir(parents=True)
    save(output / 'report.json', result)
    save(output / 'config.json', config)
    (output / Path(__file__).name).write_bytes(Path(__file__).read_bytes())
    lines = ['# Fixed-scale multi-seed benchmark', '', 'Primary: three fresh dataset seeds; reference is excluded from means.',
             'LightGCN optimizer runs are averaged within each dataset before aggregation.', '',
             '| Method | Recall mean | Recall sample std | nDCG mean | nDCG sample std | nDCG min | nDCG max |',
             '|---|---:|---:|---:|---:|---:|---:|']
    for method in METHODS:
        r, n = summary[method]['recall'], summary[method]['ndcg']
        lines.append(f"| {method} | {r['mean']:.8f} | {r['std']:.8f} | {n['mean']:.8f} | {n['std']:.8f} | {n['min']:.8f} | {n['max']:.8f} |")
    lines += ['', '| Dataset seed | Role | Likes | Test users | Method | Recall@10 | nDCG@10 | Within-dataset optimizer nDCG std |',
              '|---|---|---:|---:|---|---:|---:|---:|']
    for d in loaded:
        for method in METHODS:
            m = d['scores'][method]
            users = d['test_results'][0]['all']['users']
            lines.append(f"| {d['seed']} | {'reference' if d is reference else 'primary'} | {d['counts']['likes']} | {users} | {method} | {m['recall']['mean']:.8f} | {m['ndcg']['mean']:.8f} | {m['ndcg']['std']:.8f} |")
    lines += ['', '| Left minus right | nDCG mean delta | nDCG wins/ties/losses | Recall mean delta | Recall wins/ties/losses |',
              '|---|---:|---|---:|---|']
    for pair in differences:
        n, r = (pair['left_minus_right'][metric] for metric in ('ndcg', 'recall'))
        lines.append(f"| {pair['left']} - {pair['right']} | {n['mean']:.8f} | {n['wins']}/{n['ties']}/{n['losses']} | {r['mean']:.8f} | {r['wins']}/{r['ties']}/{r['losses']} |")
    lines += ['', '## Validation-selected parameters', '', '| Dataset seed | EASE lambda | PPR alpha | LightGCN layers | Learning rate | Epochs |',
              '|---|---:|---:|---:|---:|---:|']
    for d in loaded:
        s = d['selections']
        g = s['lightgcn']['parameters']
        lines.append(f"| {d['seed']} | {s['ease']['parameter']} | {s['ppr']['parameter']} | {g['layers']} | {g['learning_rate']} | {g['epochs']} |")
    lines += ['', 'Full precision per-optimizer metrics, selections and artifact fingerprints: report.json.', '',
              '## Limits', '', *['- ' + note for note in result['limitations']]]
    (output / 'review.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    save(output / 'manifest.json', {p.name: sha(p) for p in output.iterdir()})
    print(json.dumps(summary))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config', type=Path, default=Path(__file__).with_name('configs') / 'recommenders/multiseed.json')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    run(args.config, args.output)

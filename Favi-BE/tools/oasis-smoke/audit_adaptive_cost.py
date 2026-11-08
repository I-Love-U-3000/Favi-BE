"""Read-only historical cost audit. Standard library only; never imports trainers."""
import argparse
import hashlib
import json
import math
from pathlib import Path


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def unique_refits(choices, results):
    """Mirror the frozen runner cache key, excluding route threshold and label."""
    cache = {}
    for result in results:
        if 'refit_seconds' not in result:
            continue
        choice = choices[result['label']]
        key = (choice['family'], json.dumps(choice['parameters'], sort_keys=True),
               result['optimizer_seed'])
        seconds, size = result['refit_seconds'], result['model_state_bytes']
        if not math.isfinite(seconds) or seconds < 0 or size < 0:
            raise ValueError('Invalid historical cost')
        if key in cache:
            if (cache[key]['seconds'], cache[key]['model_state_bytes']) != (seconds, size):
                raise ValueError('Shared refit disagrees')
            cache[key]['labels'].append(result['label'])
        else:
            cache[key] = dict(family=key[0], parameters=choice['parameters'],
                              optimizer_seed=key[2], seconds=seconds,
                              model_state_bytes=size, labels=[result['label']])
    return list(cache.values())


def audit(root):
    base = root / '.tools/benchmarks/adaptive-confirmatory-ranking-20261108'
    tracked = root / 'document/benchmarks/adaptive-confirmatory-ranking-20261108'
    batch = load(base / 'batch.json')
    if batch['status'] != 'PASS' or len(batch['datasets']) != 6:
        raise ValueError('Require all six original runs')
    inventory, runs = {}, []

    def verify(path, expected=None):
        actual = sha(path)
        if expected is not None and actual != expected:
            raise ValueError('Hash mismatch: ' + str(path))
        inventory[path.relative_to(root).as_posix()] = actual
        return actual

    verify(base / 'batch.json', sha(tracked / 'batch.json'))
    for entry in batch['datasets']:
        name = entry['dataset']
        folder = base / name
        verify(folder / 'manifest.json', entry['manifest_sha256'])
        manifest = load(folder / 'manifest.json')
        for member, expected in manifest.items():
            verify(folder / member, expected)
        verify(folder / 'report.json', entry['report_sha256'])
        report, selection = load(folder / 'report.json'), load(folder / 'selection.json')
        verify(folder / 'selection.json', report['selection_sha256'])
        for member in ('manifest.json', 'report.json', 'selection.json', 'tuning.json'):
            if sha(folder / member) != sha(tracked / name / member):
                raise ValueError('Tracked evidence differs: ' + name + '/' + member)
        log = base / (name + '.log')
        verify(log)
        # Progress lines record trial order/scores, but contain no timestamps.
        trials = load(folder / 'tuning.json')
        refits = unique_refits(selection['choices'], report['results'])
        terminals = {str(i): max(t['epoch'] for t in trials
                     if t['family'] == 'lightgcn' and t['grid_index'] == i) for i in range(4)}
        runs.append(dict(dataset=name, elapsed_seconds=entry['elapsed_seconds'],
            validation_records=len(trials), lightgcn_terminal_epochs=terminals,
            unique_refits=refits, unique_refit_seconds=sum(r['seconds'] for r in refits),
            naive_label_refit_seconds=sum(r.get('refit_seconds', 0) for r in report['results']),
            refit_model_state_bytes_sum=sum(r['model_state_bytes'] for r in refits),
            refit_model_state_bytes_max=max(r['model_state_bytes'] for r in refits),
            original_search_seconds=None, original_training_peak_rss_bytes=None))
    export = root / '.tools/feed-experiment/adaptive-export-20261108-main11-250'
    for name, expected in load(export / 'manifest.json').items():
        verify(export / name, expected)
    provenance = load(export / 'provenance.json')
    verify(export / 'manifest.json')
    verify(export / 'artifact.json', provenance['artifact_sha256'])
    http_path = root / 'document/benchmarks/adaptive-http-20261108/summary.json'
    verify(http_path)
    http = load(http_path)
    fresh = next(r for r in http['runs'] if r['mode'] == 'fresh')
    return dict(status='PASS', scope='historical evidence audit; no model execution',
        original_source_commit=batch['git_commit'], runs=runs,
        total_elapsed_seconds=sum(r['elapsed_seconds'] for r in runs),
        total_unique_refit_seconds=sum(r['unique_refit_seconds'] for r in runs),
        export={k: provenance[k] for k in ('artifact_bytes', 'users', 'refits',
                'export_and_replay_seconds', 'total_seconds')},
        serving=dict(restarts=fresh['restarts'], memory=fresh['memory'],
                     direct=fresh['direct'], gateway=fresh['gateway']),
        missing=dict(original_search_seconds=None, original_validation_fit_only_seconds=None,
                     original_training_peak_rss_bytes=None, artifact_load_distribution_ms=None,
                     energy_kwh=None, monetary_cost=None),
        limitations=['Elapsed includes gate, validation, refit, evaluation, diagnostics, I/O, up to20s polling and post-run hash checks',
                     'Refit timings exclude InteractionData construction and feed/evaluation work',
                     'Model-state byte sums repeat shared InteractionData CSR storage across distinct models; not physical cache allocation or peak resident memory',
                     'No subtraction of refit from elapsed can isolate search or training',
                     'A new cost-only replay cannot recover historical phase times or peak RAM',
                     'Primary practical relevance FAIL; gateway load FAIL; artifact250users not full5k'],
        source_sha256=inventory)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = audit(args.root.resolve())
    # Exclusive creation protects retained evidence from accidental overwrite.
    with args.output.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(result, indent=2, allow_nan=False) + '\n')
    print(json.dumps({k: result[k] for k in ('status', 'total_elapsed_seconds', 'total_unique_refit_seconds')}))

"""Independent stdlib audit of saved validation decisions, without model imports."""
import argparse
import hashlib
import itertools
import json
import math
from pathlib import Path


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def winner(records, families, threshold, tolerance):
    eligible = [r for r in records if r['family'] in families and
                (threshold is None or r['threshold'] == threshold)]
    require(bool(eligible), 'Empty selection population')
    maximum = max(r['ndcg'] for r in eligible)
    # Compare each candidate with the global maximum, never a rolling incumbent.
    tied = [r for r in eligible if maximum - r['ndcg'] <= tolerance]
    return min(tied, key=lambda r: (r['threshold'], families.index(r['family']),
                                   r['grid_index'], r['epoch']))


def audit(records, choices, config):
    require(bool(records), 'Missing trials')
    require(all(type(r['ndcg']) in (int, float) and math.isfinite(r['ndcg'])
                and 0 <= r['ndcg'] <= 1 for r in records), 'Invalid nDCG')
    require(len({r['population_sha256'] for r in records}) == 1,
            'Validation populations differ')
    actual = {}
    for r in records:
        key = (r['family'], r['grid_index'], r['epoch'], r['threshold'])
        require(key not in actual, 'Duplicate trial')
        actual[key] = r
    expected = {}

    def expect(family, index, epoch, threshold, parameters):
        key = (family, index, epoch, threshold)
        require(key in actual, 'Missing trial: ' + str(key))
        require(actual[key]['parameters'] == parameters, 'Grid parameters differ: ' + str(key))
        expected[key] = parameters
        return actual[key]['ndcg']

    for family, values in (('ease', config['ease_regularization']), ('ppr', config['ppr_alpha'])):
        for index, value in enumerate(values):
            for threshold in config['thresholds']:
                expect(family, index, 0, threshold, {'value': value})
    terminal_epochs = []
    for index, (layers, rate) in enumerate(itertools.product(
            config['lightgcn_layers'], config['lightgcn_learning_rates'])):
        epochs = sorted({r['epoch'] for r in records
                         if r['family'] == 'lightgcn' and r['grid_index'] == index})
        require(bool(epochs), 'Missing LightGCN trajectory')
        require(epochs == list(range(config['lightgcn_check_every'], epochs[-1] + 1,
                                     config['lightgcn_check_every'])) and
                epochs[-1] <= config['lightgcn_max_epochs'], 'Checkpoint sequence differs')
        best = {t: -math.inf for t in config['thresholds']}
        stale = {t: 0 for t in config['thresholds']}
        for epoch in epochs:
            require(not all(n >= config['lightgcn_patience'] for n in stale.values()),
                    'Trajectory continued after dual patience')
            parameters = dict(dimensions=config['lightgcn_dimensions'], layers=layers,
                              learning_rate=rate, regularization=config['lightgcn_regularization'],
                              epochs=epoch, check_every=config['lightgcn_check_every'])
            for threshold in config['thresholds']:
                score = expect('lightgcn', index, epoch, threshold, parameters)
                if score > best[threshold]:
                    best[threshold], stale[threshold] = score, 0
                else:
                    stale[threshold] += 1
        require(epochs[-1] == config['lightgcn_max_epochs'] or
                all(n >= config['lightgcn_patience'] for n in stale.values()),
                'Premature trajectory termination')
        terminal_epochs.append(epochs[-1])
    v2 = config['formula_v2']
    for index, (social, decay, cap) in enumerate(itertools.product(
            v2['social_weight'], v2['decay'], v2['author_cap'])):
        expect('formula-v2', index, 0, 1,
               dict(social_weight=social, decay=decay, author_cap=cap))
    require(set(actual) == set(expected), 'Unexpected trial outside grid')
    families = config['method_order']
    scopes = {'B1': (families, 1), 'R': (families, None),
              'formula-v2': (['formula-v2'], 1)}
    scopes.update({family: ([family], 1) for family in families})
    require(set(choices) == set(scopes), 'Choice labels differ')
    for label, (allowed, threshold) in scopes.items():
        require(choices[label] == winner(records, allowed, threshold, config['selection_tolerance']),
                'Selection is not protocol winner: ' + label)
    return dict(trials=len(records), choices=len(choices), terminal_epochs=terminal_epochs)


def run(root):
    benchmarks = root / 'document' / 'benchmarks'
    config_path = root / 'tools' / 'oasis-smoke' / 'configs' / 'recommenders' / 'adaptive-v1.json'
    config = read(config_path)
    results, hashes = [], {}
    for scenario in config['scenarios']:
        for seed in config['dataset_seeds']:
            folder = benchmarks / 'adaptive-confirmatory-ranking-20261108' / f'adaptive-confirmatory-{scenario}-{seed}'
            paths = [folder / 'tuning.json', folder / 'selection.json']
            for path in paths:
                hashes[path.relative_to(root).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
            result = audit(read(paths[0]), read(paths[1])['choices'], config)
            results.append(dict(scenario=scenario, seed=seed, **result))
    return dict(status='PASS', scope='Saved validation grid, dual patience and global-max selection only; no model rerun',
                datasets=results, config_sha256=hashlib.sha256(config_path.read_bytes()).hexdigest(),
                inputs_sha256=hashes)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), 'Output exists; preserve prior receipt')
    result = run(Path(__file__).resolve().parents[3])
    args.output.write_text(json.dumps(result, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(json.dumps(result, indent=2))

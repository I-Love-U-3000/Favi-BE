"""Bounded warm-rank export from frozen adaptive selections; never retune."""
import os
for variable in ('OPENBLAS_NUM_THREADS', 'MKL_NUM_THREADS', 'OMP_NUM_THREADS'):
    os.environ[variable] = '4'

import argparse
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path
import time

from adaptive_feed import AdaptiveFeed
from analyze_adaptive_cohorts import analyze
from build_feed_artifact import mapping
from collaborative_models import InteractionData
from dataset_policy import fingerprint
from run_adaptive_benchmark import SOURCE_NAMES, refit, save, sha
from run_adaptive_replay import read, verify_manifest
from source_provenance import verify_dependency_lock


def export_rows(data, onboarding, models, users, posts, max_users):
    """Save warm ranks even below threshold: live routing belongs to HTTP."""
    if not 1 <= max_users <= 1000:
        raise ValueError('Require 1..1000 serving users')
    arrived = sorted(r['user_id'] for r in onboarding if r['arrival_tick'] < data.cutoff)
    if len(arrived) != len(set(arrived)):
        raise ValueError('Duplicate arrived user')
    if not set(arrived) <= users.keys() or not set(data.items) <= posts.keys():
        raise ValueError('Incomplete source GUID mappings')
    rows = []
    for user in arrived[:max_users]:
        row = dict(userId=users[user], ranks={})
        for label in ('B1', 'R'):
            model = models[label]
            # A zero-history user has no learned user representation. An empty
            # row forces missing-history fallback after their first live like.
            ranked = model.rank(user, 300) if user in data.user_index else []
            scores = model.scores(user) if ranked else None
            values = [dict(postId=posts[item], score=float(scores[data.item_index[item]])) for item in ranked]
            if any(not math.isfinite(p['score']) for p in values):
                raise ValueError('Nonfinite exported score')
            row['ranks'][label] = values
        rows.append(row)
    return rows


def build(dataset, ranking, output, max_users=1000, optimizer_seed=20261103):
    dataset, ranking, output = dataset.resolve(), ranking.resolve(), output.resolve()
    if output.exists() or output.is_relative_to(dataset) or output.is_relative_to(ranking):
        raise ValueError('Require fresh output outside inputs')
    if not 1 <= max_users <= 1000:
        raise ValueError('Require 1..1000 serving users')
    started = time.perf_counter()
    manifest_hash = verify_manifest(ranking)
    config = read(ranking / 'protocol.json')
    if config != read(Path(__file__).with_name('configs') / 'recommenders' / 'adaptive-v1.json'):
        raise ValueError('Frozen protocol mismatch')
    if optimizer_seed != config['test_training_seeds'][0]:
        raise ValueError('Serving optimizer fixed to first declared seed, never best test seed')
    for name in SOURCE_NAMES:
        if sha(Path(__file__).with_name(name)) != read(ranking / 'source-inventory.json')[name]:
            raise ValueError('Frozen source drift: ' + name)
    gate = analyze(dataset)
    if not gate['gate_passed']:
        raise ValueError('Dataset gate failed')
    lock = verify_dependency_lock(Path(__file__).with_name('requirements-oasis.lock.txt'))
    selection = read(ranking / 'selection.json')
    selection_hash = sha(ranking / 'selection.json')
    if selection['test_not_used'] is not True or selection['config_sha256'] != sha(ranking / 'protocol.json'):
        raise ValueError('Unfrozen selection')
    if read(ranking / 'report.json')['selection_sha256'] != selection_hash:
        raise ValueError('Selection/report mismatch')
    cutoff = config['cutoffs'][1]
    events = [json.loads(line) for line in (dataset / 'events.jsonl').read_text().splitlines()]
    prefix = [e for e in events if e['tick'] < cutoff]
    onboarding = read(dataset / 'onboarding.json')
    if fingerprint(prefix) != selection['prefix_sha256'] or sha(dataset / 'onboarding.json') != selection['onboarding_sha256']:
        raise ValueError('Selection/dataset mismatch')
    users = mapping(dataset / 'export/users.csv', 'oasis_user_id', 'user_id')
    posts = mapping(dataset / 'export/posts.csv', 'oasis_post_id', 'post_id')
    mapping_hash = hashlib.sha256((dataset / 'export/users.csv').read_bytes() + b'\0' +
                                  (dataset / 'export/posts.csv').read_bytes()).hexdigest()
    choices = {label: selection['choices'][label] for label in ('B1', 'R')}
    if choices['B1']['threshold'] != 1 or any(c['threshold'] not in (1, 5) for c in choices.values()):
        raise ValueError('Unsupported routing selection')
    data, models, cache, fits = InteractionData(prefix, cutoff), {}, {}, []
    output.mkdir(parents=True)
    # Preserve failure evidence after preflight; no overwrite on retry.
    save(output / 'inputs.json', dict(dataset=str(dataset), ranking=str(ranking),
         ranking_manifest_sha256=manifest_hash, selection_sha256=selection_hash,
         optimizer_seed=optimizer_seed, max_users=max_users))
    for label, choice in choices.items():
        seed = optimizer_seed if choice['family'] == 'lightgcn' else None
        key = (choice['family'], json.dumps(choice['parameters'], sort_keys=True), seed)
        if key not in cache:
            begun = time.perf_counter()
            cache[key] = refit(data, choice, seed)
            fits.append(dict(family=choice['family'], parameters=choice['parameters'],
                             optimizer_seed=seed, seconds=time.perf_counter() - begun,
                             model_state_bytes=cache[key].state_bytes()))
        models[label] = cache[key]
    begun = time.perf_counter()
    rows = export_rows(data, onboarding, models, users, posts, max_users)
    # Verify the refitted system's top10 against immutable original predictions
    # for every exported target user, including the live-cold route.
    for label, choice in choices.items():
        suffix = '-' + str(optimizer_seed) if choice['family'] == 'lightgcn' else ''
        original = {r['user_id']: r['recommended'] for r in
                    (json.loads(line) for line in (ranking / f'test-{label}{suffix}-users.jsonl').read_text().splitlines())}
        feed = AdaptiveFeed(prefix, cutoff, onboarding, method='collaborative',
                            warm_model=models[label], threshold=choice['threshold'])
        selected = sorted(r['user_id'] for r in onboarding if r['arrival_tick'] < cutoff)[:max_users]
        for user in selected:
            if user in original and feed.rank(user, config['k']) != original[user]:
                raise ValueError('Frozen refit prediction mismatch')
    methods = {label: dict(family=c['family'], threshold=c['threshold'], parameters=c['parameters'],
                           epoch=c['epoch'], optimizerSeed=optimizer_seed if c['family'] == 'lightgcn' else None)
               for label, c in choices.items()}
    artifact = dict(schemaVersion=2, version=selection_hash + '-adaptive', createdAtUtc=datetime.now(timezone.utc).isoformat(),
                    trainingCutoffTick=cutoff, datasetEventsSha256=sha(dataset / 'events.jsonl'),
                    selectionSha256=selection_hash, sourceMappingSha256=mapping_hash, methods=methods, users=rows)
    encoded = (json.dumps(artifact, allow_nan=False, separators=(',', ':')) + '\n').encode('utf-8')
    if len(encoded) > 16 * 1024 * 1024:
        raise ValueError('Artifact exceeds 16 MiB; use a smaller explicit sample')
    (output / 'artifact.json').write_bytes(encoded)
    if sha(ranking / 'selection.json') != selection_hash or verify_manifest(ranking) != manifest_hash:
        raise ValueError('Input evidence changed during export')
    save(output / 'provenance.json', dict(schema_version=2, artifact_sha256=sha(output / 'artifact.json'),
         artifact_bytes=len(encoded), users=len(rows), sample_rule='ascending arrived source IDs',
         source_mapping_sha256=mapping_hash, selection_sha256=selection_hash, methods=methods,
         refits=fits, export_and_replay_seconds=time.perf_counter()-begun,
         total_seconds=time.perf_counter()-started, locked_dependencies=lock,
         limitations=['Bounded sample, not full5k serving', 'Warm ranks are frozen, current history only routes and excludes',
                      'Training/search cost belongs to original run; these timings are export refits',
                      'Peak RAM and HTTP gates pending; model_state_bytes is not process RAM']))
    (output / Path(__file__).name).write_bytes(Path(__file__).read_bytes())
    save(output / 'manifest.json', {p.name: sha(p) for p in output.iterdir() if p.is_file()})
    verify_manifest(output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('dataset', 'ranking', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--max-users', type=int, default=1000)
    args = parser.parse_args()
    build(args.dataset, args.ranking, args.output, args.max_users)

import csv
import json
from pathlib import Path
import tempfile
import unittest
import uuid
from unittest.mock import patch

import build_adaptive_artifact as exporter
from collaborative_models import InteractionData, fit_ease
import run_adaptive_benchmark as benchmark
from test_adaptive_benchmark import fixture


class AdaptiveArtifactTests(unittest.TestCase):
    def test_mapping_manifest_hashes_and_identity(self):
        with tempfile.TemporaryDirectory() as folder:
            dataset = Path(folder)
            (dataset / 'export').mkdir()
            files = {}
            for name in ('export/users.csv', 'export/posts.csv'):
                (dataset / name).write_bytes(b'original')
                files[name] = benchmark.sha(dataset / name)
            manifest = dataset / 'dataset-manifest.json'
            benchmark.save(manifest, dict(schema_version=1, files=files))
            identity = exporter.verify_mapping_inputs(dataset)
            for name in files:
                (dataset / name).write_bytes(b'changed')
                with self.assertRaisesRegex(ValueError, 'mapping changed'):
                    exporter.verify_mapping_inputs(dataset)
                (dataset / name).write_bytes(b'original')
            benchmark.save(manifest, dict(schema_version=1, files={k.replace('/', '\\'): v for k, v in files.items()}))
            self.assertNotEqual(identity, exporter.verify_mapping_inputs(dataset))
            benchmark.save(manifest, dict(schema_version=1, files={**files, 'export\\users.csv': files['export/users.csv']}))
            with self.assertRaisesRegex(ValueError, 'mapping manifest coverage'):
                exporter.verify_mapping_inputs(dataset)

    def test_warm_ranks_below_threshold_and_unknown_history_are_distinct(self):
        events, rows, _ = fixture()
        data = InteractionData(events, 300)
        model = fit_ease(data, 10)
        users = {u: str(uuid.UUID(int=u+1)) for u in range(3)}
        posts = {i: str(uuid.UUID(int=i+100)) for i in data.items}
        result = exporter.export_rows(data, rows, {'B1': model, 'R': model}, users, posts, 1000)
        self.assertEqual(result[0]['ranks']['R'][0]['postId'], posts[model.rank(0, 300)[0]])
        self.assertEqual(result[0]['ranks']['B1'], result[0]['ranks']['R'])
        self.assertEqual(result[2]['ranks']['R'], [])
        self.assertNotIn(posts[1], [p['postId'] for p in result[0]['ranks']['R']])
        self.assertEqual(len(exporter.export_rows(data, list(reversed(rows)), {'B1': model, 'R': model}, users, posts, 1)), 1)
        with self.assertRaisesRegex(ValueError, 'mappings'):
            exporter.export_rows(data, rows, {}, {}, posts, 1)
        with self.assertRaises(ValueError):
            exporter.export_rows(data, rows, {}, users, posts, 1001)

    def test_real_refits_export_frozen_choices_guid_mapping_and_replay(self):
        events, rows, config = fixture()
        rows.append(dict(user_id=3, arrival_tick=-1, preference_tick=-1, topics=[]))
        for family, parameters, epoch in (
            ('ease', {'value': 10}, 0), ('ppr', {'value': .85}, 0),
            ('lightgcn', dict(dimensions=4, layers=1, learning_rate=.01,
                             regularization=.0001, epochs=10, check_every=10), 10)):
            with self.subTest(family=family), tempfile.TemporaryDirectory() as folder:
                root = Path(folder)
                dataset, ranking, output = [root / n for n in ('dataset', 'ranking', 'artifact')]
                (dataset / 'export').mkdir(parents=True)
                benchmark.save(dataset / 'config.resolved.json', dict(users=100, ticks=500))
                benchmark.save(dataset / 'onboarding.json', rows)
                (dataset / 'events.jsonl').write_text(''.join(json.dumps(e)+'\n' for e in events))
                for filename, source, target, ids in (
                    ('users.csv', 'oasis_user_id', 'user_id', range(4)),
                    ('posts.csv', 'oasis_post_id', 'post_id', range(1, 13))):
                    with (dataset / 'export' / filename).open('w', newline='') as stream:
                        writer = csv.writer(stream)
                        writer.writerow([source, target])
                        writer.writerows((i, str(uuid.UUID(int=i+100))) for i in ids)
                config_path = root / 'config.json'
                mapping_files = {name: benchmark.sha(dataset / name) for name in
                                 ('export/users.csv', 'export/posts.csv')}
                benchmark.save(dataset / 'dataset-manifest.json', dict(schema_version=1, files=mapping_files))
                benchmark.save(config_path, config)
                choice = dict(family=family, parameters=parameters, epoch=epoch, threshold=1)
                choices = dict(B1=choice, R={**choice, 'threshold': 5})
                gate = dict(gate_passed=True, required_cohorts=[])
                # Orchestration fixture replaces data qualification, lock and
                # tuning; actual fits, original predictions and replay are real.
                with patch.object(benchmark, 'analyze', return_value=gate), patch.object(benchmark, 'verify_dependency_lock', return_value={}), patch.object(benchmark, 'tune', return_value=(choices, [])):
                    benchmark.run(dataset, ranking, config_path)
                with patch.object(exporter, 'analyze', return_value=gate), patch.object(exporter, 'verify_dependency_lock', return_value={}), patch.object(exporter, 'refit', wraps=benchmark.refit) as refit:
                    exporter.build(dataset, ranking, output, 4)
                    self.assertEqual(refit.call_count, 1)
                artifact = exporter.read(output / 'artifact.json')
                self.assertEqual(artifact['schemaVersion'], 2)
                self.assertEqual(artifact['methods']['R']['threshold'], 5)
                self.assertEqual(artifact['users'][3]['ranks']['R'], [])
                self.assertEqual(artifact['users'][0]['userId'], str(uuid.UUID(int=100)))
                exporter.verify_manifest(output)
                for missing in mapping_files:
                    with self.subTest(missing_mapping_manifest_entry=missing):
                        benchmark.save(dataset / 'dataset-manifest.json', dict(schema_version=1,
                            files={k: v for k, v in mapping_files.items() if k != missing}))
                        with patch.object(exporter, 'analyze', return_value=gate), patch.object(exporter, 'verify_dependency_lock', return_value={}), patch.object(exporter, 'refit', side_effect=AssertionError('Refit must not start')):
                            with self.assertRaisesRegex(ValueError, 'mapping manifest coverage'):
                                exporter.build(dataset, ranking, root / ('missing-mapping-' + Path(missing).name))
                        self.assertFalse((root / ('missing-mapping-' + Path(missing).name)).exists())
                benchmark.save(dataset / 'dataset-manifest.json', dict(schema_version=1, files=mapping_files))
                for name in mapping_files:
                    path = dataset / name
                    original_bytes = path.read_bytes()
                    def changed_mapping(*args, **kwargs):
                        model = benchmark.refit(*args, **kwargs)
                        path.write_bytes(original_bytes + b'\n')
                        return model
                    with self.subTest(mapping_changed_during_refit=name):
                        changed_output = root / ('changed-mapping-' + path.name)
                        with patch.object(exporter, 'analyze', return_value=gate), patch.object(exporter, 'verify_dependency_lock', return_value={}), patch.object(exporter, 'refit', side_effect=changed_mapping):
                            with self.assertRaisesRegex(ValueError, 'mapping changed'):
                                exporter.build(dataset, ranking, changed_output, 4)
                        self.assertFalse((changed_output / 'manifest.json').exists())
                    path.write_bytes(original_bytes)
                manifest_path = dataset / 'dataset-manifest.json'
                manifest_bytes = manifest_path.read_bytes()
                def changed_manifest(*args, **kwargs):
                    model = benchmark.refit(*args, **kwargs)
                    manifest_path.write_bytes(manifest_bytes + b'\n')
                    return model
                with patch.object(exporter, 'analyze', return_value=gate), patch.object(exporter, 'verify_dependency_lock', return_value={}), patch.object(exporter, 'refit', side_effect=changed_manifest):
                    with self.assertRaisesRegex(ValueError, 'Dataset manifest changed'):
                        exporter.build(dataset, ranking, root / 'changed-dataset-manifest', 4)
                self.assertFalse((root / 'changed-dataset-manifest' / 'manifest.json').exists())
                manifest_path.write_bytes(manifest_bytes)
                with self.subTest(manifest_coverage=family):
                    manifest_path = ranking / 'manifest.json'
                    manifest = exporter.read(manifest_path)
                    suffix = '-20261103' if family == 'lightgcn' else ''
                    for missing in ('selection.json', 'protocol.json', 'source-inventory.json',
                                    'report.json', f'test-B1{suffix}-users.jsonl', f'test-R{suffix}-users.jsonl', 'ALL'):
                        with self.subTest(missing_manifest_entry=missing):
                            benchmark.save(manifest_path, {} if missing == 'ALL' else {k: v for k, v in manifest.items() if k != missing})
                            with patch.object(exporter, 'analyze', return_value=gate), patch.object(exporter, 'verify_dependency_lock', return_value={}), patch.object(exporter, 'refit', side_effect=AssertionError('Refit must not start')):
                                with self.assertRaisesRegex(ValueError, 'manifest coverage'):
                                    exporter.build(dataset, ranking, root / ('missing-' + missing))
                            self.assertFalse((root / ('missing-' + missing)).exists())
                    benchmark.save(manifest_path, manifest)
                with self.assertRaisesRegex(ValueError, 'fresh output'):
                    exporter.build(dataset, ranking, output)
                with self.assertRaisesRegex(ValueError, 'first declared seed'):
                    exporter.build(dataset, ranking, root / 'wrong-seed', optimizer_seed=20261104)
                (ranking / 'selection.json').write_text('{}')
                with self.assertRaisesRegex(ValueError, 'manifest mismatch'):
                    exporter.build(dataset, ranking, root / 'tampered')


if __name__ == '__main__':
    unittest.main()

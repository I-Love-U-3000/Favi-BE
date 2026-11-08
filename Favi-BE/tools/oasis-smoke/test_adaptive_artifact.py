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
                with self.assertRaisesRegex(ValueError, 'fresh output'):
                    exporter.build(dataset, ranking, output)
                with self.assertRaisesRegex(ValueError, 'first declared seed'):
                    exporter.build(dataset, ranking, root / 'wrong-seed', optimizer_seed=20261104)
                (ranking / 'selection.json').write_text('{}')
                with self.assertRaisesRegex(ValueError, 'manifest mismatch'):
                    exporter.build(dataset, ranking, root / 'tampered')


if __name__ == '__main__':
    unittest.main()

import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from test_adaptive_benchmark import fixture
import run_adaptive_benchmark as benchmark
import run_adaptive_replay as replay


class AdaptiveReplayTests(unittest.TestCase):
    def test_variants_preserve_identity_and_remove_only_topics(self):
        _, primary, _ = fixture()
        sidecars = {'noisy': [{**r, 'topics': ['music']} for r in primary]}
        variants = replay.variants(primary, sidecars)
        self.assertEqual(variants['no-topics'][0]['topics'], [])
        self.assertEqual(primary[0]['topics'], ['food'])
        bad = copy.deepcopy(sidecars)
        bad['noisy'][0]['arrival_tick'] = 0
        with self.assertRaisesRegex(ValueError, 'population'):
            replay.variants(primary, bad)

    def test_ablation_retains_joint_parameters_and_epoch(self):
        choice = dict(family='lightgcn', parameters={'epochs': 60}, epoch=60, threshold=5)
        choices = {'B1': {**choice, 'threshold': 1}, 'R': choice}
        primary = list(replay.tasks(choices, 'primary'))
        self.assertEqual(primary[-1], ('R-no-routing', choice, 1))
        self.assertIs(primary[-1][1], choice)
        self.assertEqual([t[0] for t in replay.tasks(choices, 'no-topics')], ['B1', 'R'])
        self.assertEqual(choice['threshold'], 5)

    def test_paired_check_rejects_population_and_prediction_drift(self):
        rows = [dict(user_id=1, candidate_sha256='abc', relevant=[2], recommended=[3])]
        replay.paired_check(rows, rows, True)
        for key, value in (('user_id', 2), ('candidate_sha256', 'x'), ('relevant', [3]), ('recommended', [2])):
            with self.assertRaises(ValueError):
                replay.paired_check([{**rows[0], key: value}], rows, True)

    def test_end_to_end_fixed_selection_source_hash_and_primary_parity(self):
        events, rows, config = fixture()
        events.append(dict(kind='like', user_id=3, post_id=8, tick=410))
        # Validator requires full 100-user identity; only 3 users have targets.
        rows += [dict(user_id=u, arrival_tick=-1, preference_tick=-1, topics=[]) for u in range(3, 100)]
        rows[3]['topics'] = ['food']
        gate = dict(gate_passed=True, required_cohorts=[])
        choice = dict(family='lightgcn', parameters=dict(dimensions=4, layers=1,
                      learning_rate=.01, regularization=.0001, epochs=10,
                      check_every=10), epoch=10, threshold=1)
        choices = {'B1': choice, 'R': {**choice, 'threshold': 5}}
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            dataset, ranking, output = root / 'dataset', root / 'ranking', root / 'replay'
            dataset.mkdir()
            benchmark.save(dataset / 'config.resolved.json', dict(users=100, ticks=500))
            benchmark.save(dataset / 'onboarding.json', rows)
            for mode in config['robustness_preferences']:
                benchmark.save(dataset / f'onboarding.{mode}.json', rows if mode == 'informative' else [{**r, 'topics': ['music'] if r['topics'] else []} for r in rows])
            (dataset / 'events.jsonl').write_text(''.join(json.dumps(e) + '\n' for e in events))
            config_path = root / 'config.json'
            benchmark.save(config_path, config)
            with patch.object(benchmark, 'analyze', return_value=gate), patch.object(benchmark, 'verify_dependency_lock', return_value={}), patch.object(benchmark, 'tune', return_value=(choices, [])):
                benchmark.run(dataset, ranking, config_path)
            selection_hash = benchmark.sha(ranking / 'selection.json')
            with patch.object(replay, 'analyze', return_value=gate), patch.object(replay, 'verify_dependency_lock', return_value={}), patch.object(replay, 'refit', wraps=benchmark.refit) as refit:
                replay.run(dataset, ranking, output)
                self.assertEqual(refit.call_count, 3)
                self.assertEqual([c.args[2] for c in refit.call_args_list], config['test_training_seeds'])
                self.assertTrue(all(c.args[1]['parameters']['epochs'] == 10 for c in refit.call_args_list))
            report = replay.read(output / 'report.json')
            self.assertEqual(selection_hash, benchmark.sha(ranking / 'selection.json'))
            self.assertEqual(len(report['results']), 45)
            self.assertEqual(len({r['evaluation_population_sha256'] for r in report['results']}), 1)
            self.assertEqual(report['metadata']['no-topics']['paired_users'], 4)
            self.assertIn('C3->C0', report['metadata']['no-topics']['cohort_transitions'])
            self.assertEqual(report['metadata']['informative']['changed_topic_users'], 0)
            replay.verify_manifest(output)
            with self.assertRaisesRegex(ValueError, 'fresh output'):
                replay.run(dataset, ranking, output)
            (ranking / 'selection.json').write_text('{}')
            with self.assertRaisesRegex(ValueError, 'manifest mismatch'):
                replay.run(dataset, ranking, root / 'tampered')


if __name__ == '__main__':
    unittest.main()

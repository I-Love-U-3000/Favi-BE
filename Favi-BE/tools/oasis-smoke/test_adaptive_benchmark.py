import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from adaptive_feed import AdaptiveFeed
from benchmark_recommenders import Ranker, evaluate
from collaborative_models import InteractionData, fit_ease
import run_adaptive_benchmark as runner


def fixture():
    events = [dict(kind='post', post_id=i, user_id=10 + i % 2, topic=i % 10, tick=-1)
              for i in range(1, 13)]
    events += [dict(kind='like', user_id=u, post_id=i, tick=1)
               for u, i in ((0, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6))]
    events += [dict(kind='like', user_id=u, post_id=7, tick=310) for u in (0, 1, 2)]
    events += [dict(kind='like', user_id=u, post_id=8, tick=410) for u in (0, 1, 2)]
    rows = [dict(user_id=u, arrival_tick=-1, preference_tick=-1, topics=['food']) for u in range(3)]
    config = json.loads((Path(runner.__file__).with_name('configs') / 'recommenders' / 'adaptive-v1.json').read_text())
    return events, rows, config


class AdaptiveBenchmarkTests(unittest.TestCase):
    def test_global_max_tolerance_prevents_chained_drift(self):
        records = [dict(family='ease', threshold=1, grid_index=i, epoch=0, ndcg=s)
                   for i, s in enumerate((0.5, 0.5 + .75e-12, 0.5 + 1.5e-12))]
        self.assertEqual(runner.select(records)['grid_index'], 1)
        records += [dict(family='ppr', threshold=5, grid_index=0, epoch=0, ndcg=.9)]
        self.assertEqual(runner.select(records)['family'], 'ppr')
        self.assertEqual(runner.select(records, threshold=1)['grid_index'], 1)
        with self.assertRaises(ValueError):
            runner.select([{**records[0], 'ndcg': float('nan')}])

    def test_tie_order_threshold_family_grid_epoch(self):
        records = [dict(family=f, threshold=t, grid_index=g, epoch=e, ndcg=.2)
                   for f, t, g, e in itertools_product()]
        selected = runner.select(list(reversed(records)))
        self.assertEqual((selected['threshold'], selected['family'], selected['grid_index'], selected['epoch']), (1, 'ease', 0, 10))

    def test_dual_patience_keeps_stale_threshold_and_resets(self):
        state = runner.DualPatience([1, 5], 2)
        self.assertTrue(state.update({1: .5, 5: .1}))
        self.assertTrue(state.update({1: .5, 5: .2}))
        self.assertTrue(state.update({1: .5, 5: .3}))
        self.assertEqual(state.stale[1], 2)
        self.assertTrue(state.update({1: .6, 5: .3}))
        self.assertTrue(state.update({1: .6, 5: .3}))
        self.assertFalse(state.update({1: .6, 5: .3}))

    def test_arrival_prefix_population_and_v2_historical_semantics(self):
        events, rows, config = fixture()
        rows[2]['arrival_tick'] = 300
        rows[2]['preference_tick'] = 300
        model = fit_ease(InteractionData(events, 300), 10)
        params = dict(social_weight=1.5, decay=0, author_cap=2)
        feeds = [AdaptiveFeed(events, 300, rows, method=m, **kwargs)
                 for m, kwargs in [('popularity', {}), ('formula', {}), ('topic-only', {}),
                                  ('formula-v2', {'parameters': params}),
                                  ('collaborative', {'warm_model': model, 'threshold': 1}),
                                  ('collaborative', {'warm_model': model, 'threshold': 5})]]
        populations = []
        for feed in feeds:
            self.assertEqual(feed.rank(2, 10), [])
            result, _ = evaluate(events, 300, 400, 'fixture', dict(k=10, hours_per_tick=1, timing_repeats=1), fitted_model=feed, measure=False)
            populations.append(result['evaluation_population_sha256'])
        self.assertEqual(len(set(populations)), 1)
        v2 = feeds[3]
        historical = Ranker(events, 300, 'favi_formula_v2', parameters=params)
        self.assertEqual(v2.rank(0, 10), historical.rank(0, 10))
        self.assertEqual(v2.rank(1, 10), historical.rank(1, 10))
        # Hard cap cannot relax even with enough eligible items.
        self.assertLessEqual(len(v2.rank(0, 10)), 4)
        rows[2]['arrival_tick'] = -1
        cold = AdaptiveFeed(events, 300, rows, method='formula-v2', parameters=params)
        self.assertEqual(cold.rank(2, 10), cold.cold(2, 10))
        self.assertEqual(AdaptiveFeed([e for e in events if e['tick'] < 400], 300, rows, method='formula-v2', parameters=params).rank(0, 10), v2.rank(0, 10))

    def test_tune_all_checkpoints_joint_selection_and_prefix_rejection(self):
        events, rows, config = fixture()
        prefix = [e for e in events if e['tick'] < 400]
        calls = []
        def fake_gcn(data, params, seed, checkpoint=None):
            model = fit_ease(data, 10)
            for epoch in (1, 10, 20, 30, 40, 50, 60, 70):
                calls.append(epoch)
                if checkpoint(epoch, model, {}) is False:
                    break
            return model, []
        def score(feed):
            return dict(all={'ndcg': .2 if feed.threshold == 1 else .3}, evaluation_population_sha256='same')
        with patch.object(runner, 'fit_lightgcn', side_effect=fake_gcn):
            choices, trials = runner.tune(prefix, rows, config, score)
        self.assertEqual(choices['R']['threshold'], 5)
        self.assertEqual(choices['B1']['threshold'], 1)
        self.assertEqual(choices['R']['family'], 'ease')
        gcn = [r for r in trials if r['family'] == 'lightgcn']
        self.assertEqual(len(gcn), 4 * 6 * 2)
        self.assertEqual({r['epoch'] for r in gcn}, {10, 20, 30, 40, 50, 60})
        self.assertEqual(sum(r['family'] == 'formula-v2' for r in trials), 12)
        with self.assertRaises(ValueError):
            runner.tune(events, rows, config, score)
        with patch.object(runner, 'fit_lightgcn', side_effect=fake_gcn):
            counter = iter(range(10000))
            with self.assertRaisesRegex(ValueError, 'populations'):
                runner.tune(prefix, rows, config, lambda f: dict(all={'ndcg': .1}, evaluation_population_sha256=str(next(counter))))

    def test_refit_exact_epoch_and_no_test_callback(self):
        events, _, _ = fixture()
        data = InteractionData(events, 400)
        parameters = dict(dimensions=4, layers=1, learning_rate=.01, regularization=.0001, epochs=20, check_every=10)
        with patch.object(runner, 'fit_lightgcn', return_value=('model', [])) as fit:
            self.assertEqual(runner.refit(data, dict(family='lightgcn', parameters=parameters), 123), 'model')
            fit.assert_called_once_with(data, parameters, 123)
        self.assertIn(7, data.seen[0])
        self.assertNotIn(8, data.seen[0])

    def test_real_numerical_tuning_and_refit_replay(self):
        events, rows, config = fixture()
        config.update(lightgcn_dimensions=4, lightgcn_max_epochs=20, lightgcn_patience=2)
        prefix = [e for e in events if e['tick'] < 400]
        def score(feed):
            return evaluate(prefix, 300, 400, 'trial', dict(k=10, hours_per_tick=1, timing_repeats=1),
                            fitted_model=feed, measure=False)[0]
        choices, trials = runner.tune(prefix, rows, config, score)
        self.assertEqual(len(trials), 14 + 16 + 12)
        self.assertEqual(len({r['population_sha256'] for r in trials}), 1)
        data = InteractionData(prefix, 400)
        for family in ('ease', 'ppr', 'lightgcn'):
            selected = choices[family]
            first = runner.refit(data, selected, config['training_seed'])
            second = runner.refit(data, selected, config['training_seed'])
            self.assertEqual(first.rank(0, 10), second.rank(0, 10))
            self.assertNotIn(7, first.rank(0, 10))

    def test_optimizer_hierarchy_dataset_weighting(self):
        result = runner.aggregate_optimizer_datasets({'a': [0, 0, 0], 'b': [1]})
        self.assertEqual(result['mean'], .5)
        self.assertAlmostEqual(result['dataset_std'], 2 ** -.5)
        self.assertEqual(result['optimizer_std']['a'], 0)

    def test_run_frozen_selection_cache_and_archive_readback(self):
        events, rows, config = fixture()
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            dataset, output = root / 'data', root / 'output'
            dataset.mkdir()
            runner.save(dataset / 'config.resolved.json', dict(ticks=500))
            runner.save(dataset / 'onboarding.json', rows)
            (dataset / 'events.jsonl').write_text(''.join(json.dumps(e) + '\n' for e in events))
            config_path = root / 'config.json'
            runner.save(config_path, config)
            parameters = dict(dimensions=4, layers=1, learning_rate=.01, regularization=0, epochs=10, check_every=10)
            choice = dict(family='lightgcn', threshold=1, parameters=parameters)
            choices = {'B1': choice, 'R': {**choice, 'threshold': 5}, 'formula-v2': dict(parameters=dict(social_weight=.5, decay=0, author_cap=2))}
            observed = []
            def fake_refit(data, selected, seed):
                self.assertTrue((output / 'selection.json').exists())
                observed.append(runner.sha(output / 'selection.json'))
                self.assertEqual(data.cutoff, 400)
                return fit_ease(data, 10)
            with patch.object(runner, 'analyze', return_value=dict(gate_passed=True, required_cohorts=[])), \
                 patch.object(runner, 'verify_dependency_lock', return_value={'fixture': '1'}), \
                 patch.object(runner, 'tune', return_value=(choices, [])), \
                 patch.object(runner, 'refit', side_effect=fake_refit) as refit:
                runner.run(dataset, output, config_path)
                self.assertEqual(refit.call_count, 3)
            self.assertEqual(len(set(observed)), 1)
            report = json.loads((output / 'report.json').read_text())
            self.assertEqual(report['selection_sha256'], observed[0])
            self.assertEqual(len({r['evaluation_population_sha256'] for r in report['results']}), 1)
            for name, expected in json.loads((output / 'manifest.json').read_text()).items():
                self.assertEqual(runner.sha(output / name), expected)
            with self.assertRaises(ValueError):
                runner.run(dataset, output, config_path)


def itertools_product():
    import itertools
    return itertools.product(('ease', 'ppr', 'lightgcn'), (1, 5), (0, 1), (10, 20))


if __name__ == '__main__':
    unittest.main()

import copy
import unittest
from audit_adaptive_cost import unique_refits


class HistoricalCostTests(unittest.TestCase):
    def test_shared_thresholds_and_family_labels_count_once_but_optimizer_seeds_separate(self):
        choices = {label: dict(family='lightgcn', parameters={'epochs': 60}, threshold=t)
                   for label, t in [('B1', 1), ('R', 5), ('lightgcn', 1)]}
        rows = [dict(label=label, optimizer_seed=seed, refit_seconds=seconds, model_state_bytes=100)
                for seed, seconds in [(3, 2.0), (4, 3.0)] for label in choices]
        records = unique_refits(choices, rows)
        self.assertEqual(len(records), 2)
        self.assertEqual(sum(r['seconds'] for r in records), 5)
        changed = copy.deepcopy(rows)
        changed[1]['refit_seconds'] = 9
        with self.assertRaisesRegex(ValueError, 'Shared refit disagrees'):
            unique_refits(choices, changed)

    def test_changed_parameters_are_distinct_and_invalid_cost_rejected(self):
        choices = {'B1': dict(family='ease', parameters={'value': 100}),
                   'R': dict(family='ease', parameters={'value': 1000})}
        rows = [dict(label=label, optimizer_seed=None, refit_seconds=1, model_state_bytes=10)
                for label in choices]
        self.assertEqual(len(unique_refits(choices, rows)), 2)
        for value in (-1, float('nan'), float('inf')):
            rows[0]['refit_seconds'] = value
            with self.assertRaises(ValueError):
                unique_refits(choices, rows)


if __name__ == '__main__':
    unittest.main()

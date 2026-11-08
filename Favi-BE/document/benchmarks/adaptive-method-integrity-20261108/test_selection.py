"""Negative controls for the independent saved-decision audit."""
import copy
from pathlib import Path
import unittest
from audit_selection import audit, read, run, winner


ROOT = Path(__file__).resolve().parents[3]


class SelectionAuditTests(unittest.TestCase):
    def setUp(self):
        self.config = read(ROOT / 'tools/oasis-smoke/configs/recommenders/adaptive-v1.json')
        folder = ROOT / 'document/benchmarks/adaptive-confirmatory-ranking-20261108/adaptive-confirmatory-main-20261111'
        self.records = read(folder / 'tuning.json')
        self.choices = read(folder / 'selection.json')['choices']

    def test_all_six_saved_datasets(self):
        receipt = run(ROOT)
        self.assertEqual(len(receipt['datasets']), 6)
        self.assertEqual(sum(d['choices'] for d in receipt['datasets']), 36)

    def test_membership_is_insufficient(self):
        self.choices['B1'] = next(r for r in self.records if r['family'] == 'ease' and
                                  r['threshold'] == 1 and r['grid_index'] == 0)
        self.assertIn(self.choices['B1'], self.records)
        with self.assertRaisesRegex(ValueError, 'not protocol winner: B1'):
            audit(self.records, self.choices, self.config)

    def test_missing_duplicate_and_wrong_grid(self):
        variants = [self.records[1:], self.records + [self.records[0]]]
        wrong = copy.deepcopy(self.records)
        wrong[0]['parameters']['value'] = 999
        variants.append(wrong)
        for records in variants:
            with self.subTest(records=len(records)), self.assertRaises(ValueError):
                audit(records, self.choices, self.config)

    def test_premature_trajectory_and_missing_threshold(self):
        for records in (
            [r for r in self.records if not (r['family'] == 'lightgcn' and r['epoch'] > 10)],
            [r for r in self.records if not (r['family'] == 'lightgcn' and
                                             r['epoch'] == 10 and r['threshold'] == 5)],
        ):
            with self.subTest(records=len(records)), self.assertRaises(ValueError):
                audit(records, self.choices, self.config)

    def test_tolerance_uses_global_maximum(self):
        records = [dict(family='ease', grid_index=i, epoch=0, threshold=1, ndcg=score)
                   for i, score in enumerate((.5, .5 + .75e-12, .5 + 1.5e-12))]
        self.assertEqual(winner(records, ['ease'], 1, 1e-12)['grid_index'], 1)

    def test_continuing_after_dual_patience_is_rejected(self):
        self.config['lightgcn_patience'] = 1
        for record in self.records:
            if record['family'] == 'lightgcn' and record['grid_index'] == 0:
                record['ndcg'] = .1
        with self.assertRaisesRegex(ValueError, 'continued after dual patience'):
            audit(self.records, self.choices, self.config)

    def test_nonfinite_and_population_drift_are_rejected(self):
        for field, value in (('ndcg', float('nan')), ('population_sha256', 'different')):
            records = copy.deepcopy(self.records)
            records[0][field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                audit(records, self.choices, self.config)


if __name__ == '__main__':
    unittest.main()

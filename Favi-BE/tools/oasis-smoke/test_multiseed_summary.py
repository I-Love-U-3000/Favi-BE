import hashlib
import json
from pathlib import Path
import tempfile
import unittest

from summarize_multiseed_benchmark import aggregate, dataset_scores, paired_differences, verify_manifest, METHODS, METRICS


def results(values=(0.2, 0.4, 0.6)):
    rows = [{'method': method, 'phase': 'test', 'evaluation_population_sha256': 'same',
             'all': dict.fromkeys(METRICS, 0.1)} for method in METHODS[:-1]]
    rows += [{'method': 'lightgcn', 'phase': 'test', 'seed': seed, 'evaluation_population_sha256': 'same',
              'all': dict.fromkeys(METRICS, value)} for seed, value in zip((1, 2, 3), values)]
    return [{'method': 'ease', 'phase': 'validation', 'evaluation_population_sha256': 'validation'}] + rows


class MultiSeedSummaryTests(unittest.TestCase):
    def test_hierarchical_aggregation_and_optimizer_variability_stay_separate(self):
        a = {'seed': 10, 'scores': dataset_scores(results(), [1, 2, 3])}
        b = {'seed': 11, 'scores': dataset_scores(results((0.7, 0.8, 0.9)), [1, 2, 3])}
        output = aggregate([a, b])['lightgcn']['ndcg']
        self.assertEqual(output['n'], 2)
        self.assertAlmostEqual(output['mean'], 0.6)
        self.assertAlmostEqual(output['std'], (0.08 ** 0.5))
        self.assertAlmostEqual(a['scores']['lightgcn']['ndcg']['std'], 0.2)

    def test_missing_optimizer_seed_or_mismatched_population_rejected(self):
        with self.assertRaises(ValueError):
            dataset_scores(results()[:-1], [1, 2, 3])
        changed = results()
        changed[-1]['evaluation_population_sha256'] = 'other'
        with self.assertRaises(ValueError):
            dataset_scores(changed, [1, 2, 3])

    def test_duplicate_dataset_seeds_rejected(self):
        d = {'seed': 1, 'scores': dataset_scores(results(), [1, 2, 3])}
        with self.assertRaises(ValueError):
            aggregate([d, d])

    def test_paired_sign_counts_use_dataset_means(self):
        a = {'seed': 10, 'scores': dataset_scores(results(), [1, 2, 3])}
        b = {'seed': 11, 'scores': dataset_scores(results((0, 0, 0)), [1, 2, 3])}
        p = next(p for p in paired_differences([a, b]) if p['left']=='popularity' and p['right']=='lightgcn')
        n = p['left_minus_right']['ndcg']
        self.assertEqual((n['wins'], n['ties'], n['losses'], n['n']), (1, 0, 1, 2))
        self.assertAlmostEqual(n['mean'], -0.1)

    def test_manifest_mutation_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            file = root / 'report.json'
            file.write_bytes(b'original')
            (root/'manifest.json').write_text(json.dumps({'report.json': hashlib.sha256(b'original').hexdigest()}))
            verify_manifest(root)
            file.write_bytes(b'changed')
            with self.assertRaises(ValueError):
                verify_manifest(root)

    def test_manifest_path_escape_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)/'run'
            root.mkdir()
            (root.parent/'outside').write_bytes(b'data')
            (root/'manifest.json').write_text(json.dumps({'../outside': hashlib.sha256(b'data').hexdigest()}))
            with self.assertRaises(ValueError):
                verify_manifest(root)


if __name__ == '__main__':
    unittest.main()

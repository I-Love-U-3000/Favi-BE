import copy
from pathlib import Path
import re
import unittest
from adaptive_onboarding import TOPICS, plan, validate, visible_preferences


class OnboardingTests(unittest.TestCase):
    def test_lifecycle_allocation_and_preference_modes(self):
        from collections import Counter
        from community_policy import population
        from adaptive_onboarding import preferences
        rows = plan(5000, 20261110, 500, 'lifecycle-v2')
        counts = Counter(r['arrival_tick'] for r in rows)
        self.assertEqual(counts[-1], 1500)
        self.assertEqual(counts[299], 750)
        self.assertEqual(counts[399], 750)
        self.assertEqual(sum(counts[t] for t in (290, 270, 240)), 1000)
        self.assertEqual(sum(counts[t] for t in (390, 370, 340)), 1000)
        people = population(20261110, 5000, 10)
        informative = preferences(rows, people, 'informative')
        noisy = preferences(rows, people, 'noisy')
        self.assertEqual(preferences(rows, people, 'independent'), rows)
        for before, after, person in zip(informative, noisy, people):
            n = len(before['topics'])
            top = sorted(range(10), key=lambda i: (-person.interests[i], i))[:n]
            self.assertEqual(before['topics'], [TOPICS[i] for i in top])
            self.assertEqual(len(set(before['topics']) & set(after['topics'])), n // 2)
            self.assertEqual(before['arrival_tick'], after['arrival_tick'])
    def test_cutoff_waves_and_preference_balance(self):
        rows = plan(1000, 20261110, 500, "cutoff-waves-v1")
        from collections import Counter
        self.assertEqual(Counter(r['arrival_tick'] for r in rows), {-1: 400, 299: 300, 399: 300})
        for tick in (299, 399):
            self.assertEqual(Counter(len(r['topics']) for r in rows if r['arrival_tick'] == tick), {0: 100, 1: 100, 3: 100})
        with self.assertRaises(ValueError):
            plan(1000, 1, arrival_policy='unknown')
    def test_production_catalog_parity(self):
        source = Path(__file__).resolve().parents[2] / "Favi-BE.Modules.Auth/Domain/InterestTopics.cs"
        catalog = re.findall(r'"([a-z]+)"', source.read_text(encoding="utf-8").split("public static string[] Normalize")[0])
        self.assertEqual(tuple(catalog[:10]), TOPICS)

    def test_deterministic_and_varied(self):
        rows = plan(5000, 20261110)
        self.assertEqual(rows, plan(5000, 20261110))
        self.assertNotEqual(rows, plan(5000, 20261111))
        self.assertEqual({len(r["topics"]) for r in rows}, {0, 1, 3})
        self.assertTrue(any(r["arrival_tick"] >= 400 for r in rows))

    def test_temporal_visibility(self):
        row = {"arrival_tick": 20, "preference_tick": 25, "topics": ["food"]}
        self.assertIsNone(visible_preferences(row, 20))
        self.assertEqual(visible_preferences(row, 25), [])
        self.assertEqual(visible_preferences(row, 26), ["food"])

    def test_reject_corruption(self):
        original = plan(100, 7)
        for field, value in (("arrival_tick", 500), ("preference_tick", -2), ("topics", ["books"]), ("topics", ["food", "food"])):
            rows = copy.deepcopy(original)
            rows[0][field] = value
            with self.assertRaises(ValueError):
                validate(rows, 100, 500)

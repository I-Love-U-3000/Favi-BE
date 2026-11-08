import copy
import json
from pathlib import Path
import random
import unittest

from community_policy import population
from dataset_policy import validate_config, audit_events, sample_feed, fingerprint


class DatasetPolicyTests(unittest.TestCase):
    def setUp(self):
        self.config = json.loads((Path(__file__).with_name("configs") / "smoke.json").read_text())
        self.events = [
            {"seq": 0, "tick": -1, "user_id": 1, "bootstrap": True, "kind": "post", "post_id": 10, "topic": 0},
            {"seq": 1, "tick": 0, "user_id": 0, "bootstrap": False, "kind": "exposure", "post_ids": [10]},
            {"seq": 2, "tick": 0, "user_id": 0, "bootstrap": False, "kind": "like", "post_id": 10, "exposure_seq": 1}]

    def test_profiles_validate_and_unknown_keys_fail(self):
        for path in Path(__file__).with_name("configs").glob("*.json"):
            validate_config(json.loads(path.read_text()))
        self.config["typo"] = 1
        with self.assertRaises(ValueError):
            validate_config(self.config)

    def test_bad_values_fail_before_runtime(self):
        for key, value in (("users", 101), ("seed", True), ("exploration", float("nan")), ("topics", ["a", "a"]), ("schema_version", 2), ("max_posts", 10)):
            config = copy.deepcopy(self.config)
            config[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                validate_config(config)

    def test_config_hash_ignores_object_key_order_but_not_seed(self):
        self.assertEqual(fingerprint(self.config), fingerprint(dict(reversed(list(self.config.items())))))
        other = {**self.config, "seed": self.config["seed"] + 1}
        self.assertNotEqual(fingerprint(self.config), fingerprint(other))

    def test_topic_population_and_feed_reproduce_without_self_duplicates(self):
        people = population(42, 100, 8)
        self.assertEqual(len(people[0].interests), 8)
        self.assertAlmostEqual(sum(people[0].interests), 1)
        posts = [{"id": i, "author": i % 100, "topic": i % 8} for i in range(150)]
        first = sample_feed(people[0], posts, people, self.config, random.Random(42))
        self.assertEqual(first, sample_feed(people[0], posts, people, self.config, random.Random(42)))
        self.assertEqual(len({p["id"] for p in first}), len(first))
        self.assertTrue(all(p["author"] != 0 for p in first))

    def test_exposure_is_not_automatically_a_positive_or_negative(self):
        report = audit_events(self.events[:2], self.config)
        self.assertEqual(report["likes"], 0)
        self.assertEqual(report["exposures"], 1)
        self.assertFalse(report["quality_passed"])

    def test_unseen_future_item_is_rejected(self):
        self.events[1]["post_ids"] = [99]
        with self.assertRaises(ValueError):
            audit_events(self.events, self.config)

    def test_interaction_requires_same_actor_prior_exposure(self):
        self.events[2]["user_id"] = 2
        with self.assertRaises(ValueError):
            audit_events(self.events, self.config)

    def test_duplicate_positive_is_rejected(self):
        self.events.append({**self.events[2], "seq": 3})
        with self.assertRaises(ValueError):
            audit_events(self.events, self.config)

    def test_time_reversal_and_false_bootstrap_are_rejected(self):
        for key, value in (("tick", -1), ("bootstrap", True), ("seq", 4)):
            events = copy.deepcopy(self.events)
            events[-1][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                audit_events(events, self.config)


if __name__ == "__main__":
    unittest.main()

import math
import unittest
from pathlib import Path
from benchmark_recommenders import Ranker, metrics, validate_protocol, evaluate
from run_formula_benchmark import grid, select_trial, paired_interval
from run_formula_benchmark import run as run_tuned
from benchmark_recommenders import run as run_fixed


def post(item, author, tick=-1):
    return {"kind": "post", "post_id": item, "user_id": author, "tick": tick}


def like(user, item, tick):
    return {"kind": "like", "user_id": user, "post_id": item, "tick": tick}


class BenchmarkTests(unittest.TestCase):
    config = {"schema_version": 1, "k": 2, "train_fraction": .6, "validation_fraction": .2, "hours_per_tick": 1, "timing_repeats": 1}

    def test_global_cutoffs_and_bad_protocol(self):
        self.assertEqual(validate_protocol(self.config, 80), (48, 64))
        with self.assertRaises(ValueError):
            validate_protocol({**self.config, "train_fraction": .9}, 80)
        with self.assertRaises(ValueError):
            validate_protocol({**self.config, "hours_per_tick": float("nan")}, 80)

    def test_future_likes_items_and_follows_cannot_change_fitted_ranking(self):
        past = [post(1, 1), post(2, 2), like(3, 1, 0)]
        future = [post(3, 4, 2), like(5, 2, 2), {"kind": "follow", "user_id": 0, "target_user_id": 2, "tick": 2}]
        for method in ("popularity", "favi_formula"):
            a, b = Ranker(past, 2, method), Ranker(past + future, 2, method)
            self.assertEqual(a.rank(0, 10), b.rank(0, 10))
            self.assertEqual(b.candidates(0), {1, 2})

    def test_self_seen_exclusion_and_zero_like_ties(self):
        model = Ranker([post(1, 0), post(2, 1), post(3, 2), post(4, 3), like(0, 2, 0)], 1, "popularity")
        self.assertEqual(model.rank(0, 10), [3, 4])

    def test_recall_and_ndcg_known_example(self):
        result = metrics([9, 2], {2, 3}, 2)
        self.assertEqual(result["recall"], .5)
        self.assertEqual(result["precision"], .5)
        self.assertAlmostEqual(result["ndcg"], (1 / math.log2(3)) / (1 + 1 / math.log2(3)))
        self.assertEqual(metrics([2, 3], {2, 3}, 2)["ndcg"], 1)
        with self.assertRaises(ValueError):
            metrics([2, 2], {2}, 2)

    def test_formula_social_affinity_and_author_damping(self):
        events = [post(1, 1), post(2, 1), post(3, 2), {"kind": "follow", "user_id": 0, "target_user_id": 1, "tick": -1}]
        model = Ranker(events, 0, "favi_formula", 0)
        self.assertEqual(model.rank(0, 3), [1, 2, 3])  #6.25,4.0625,2.5
        events.insert(2, post(4, 1))
        # Third same-author score6.25*.4 ties other-author2.5; source-ID tie break.
        self.assertEqual(Ranker(events, 0, "favi_formula", 0).rank(0, 4), [1, 2, 3, 4])

    def test_formula_time_decay_prefers_recent_equal_posts(self):
        events = [post(1, 1, -1), post(2, 2, 4)]
        self.assertEqual(Ranker(events, 5, "favi_formula", 1).rank(0, 2), [2, 1])
        self.assertEqual(Ranker(events, 5, "favi_formula", 0).rank(0, 2), [1, 2])

    def test_frozen_catalog_reports_unavailable_targets_and_cold_users(self):
        events = [post(1, 1), post(2, 2, 2), like(0, 1, 2), like(3, 2, 3)]
        result, rows = evaluate(events, 2, 4, "popularity", self.config)
        self.assertEqual(result["future_item_positive_edges_excluded"], 1)
        self.assertEqual(result["excluded_users_no_eligible_target"], 1)
        self.assertEqual(result["cold"]["users"], 1)
        self.assertEqual(result["warm"]["users"], 0)
        self.assertEqual(rows[0]["recommended"], [1])

    def test_refit_at_test_boundary_can_use_validation_not_test(self):
        events = [post(1, 1), post(2, 2), like(3, 2, 2), like(4, 1, 4)]
        self.assertEqual(Ranker(events, 2, "popularity").rank(0, 2), [1, 2])
        self.assertEqual(Ranker(events, 4, "popularity").rank(0, 2), [2, 1])

    def test_v2_hard_author_cap_and_uncapped_ablation(self):
        events = [post(i, 1 if i <= 4 else i) for i in range(1, 9)]
        parameters = {"social_weight": 1.5, "decay": 0, "author_cap": 2}
        self.assertEqual(Ranker(events, 0, "favi_formula_v2", 1, parameters).rank(0, 5), [1, 2, 5, 6, 7])
        parameters = {**parameters, "author_cap": None}
        self.assertEqual(Ranker(events, 0, "favi_formula_v2", 1, parameters).rank(0, 5), [1, 2, 3, 4, 5])

    def test_v2_rejects_missing_or_invalid_parameters(self):
        for parameters in (None, {}, {"social_weight": float("nan"), "decay": 0, "author_cap": 2}):
            with self.assertRaises(ValueError):
                Ranker([], 0, "favi_formula_v2", 1, parameters)

    def test_v2_future_events_cannot_change_ranking(self):
        past = [post(1, 1), post(2, 2), like(3, 1, 0)]
        future = [post(3, 3, 2), like(4, 2, 2)]
        for parameters in grid():
            self.assertEqual(Ranker(past, 2, "favi_formula_v2", 1, parameters).rank(0, 10),
                             Ranker(past + future, 2, "favi_formula_v2", 1, parameters).rank(0, 10))

    def test_selection_uses_validation_only_and_stable_ties(self):
        trials = [{"trial": i, "validation": {"all": {"ndcg": .2, "recall": .3}}, "test_ndcg": i} for i in range(3)]
        self.assertEqual(select_trial(trials)["trial"], 0)
        trials[1]["validation"]["all"]["ndcg"] = .4
        self.assertEqual(select_trial(trials)["trial"], 1)
        self.assertEqual(len(grid()), 12)

    def test_paired_interval_known_difference(self):
        left = [{"user_id": i, "ndcg": .1} for i in range(4)]
        right = [{"user_id": i, "ndcg": .3} for i in range(4)]
        result = paired_interval(left, right, "ndcg", repetitions=100)
        self.assertAlmostEqual(result["mean_delta"], .2)
        for bound in result["user_bootstrap_95_percentile_interval"]:
            self.assertAlmostEqual(bound, .2)

    def test_output_cannot_write_inside_immutable_dataset(self):
        root = Path(".tools/datasets/protected-input")
        for runner in (run_fixed, run_tuned):
            with self.subTest(runner=runner), self.assertRaisesRegex(ValueError, "outside the immutable dataset"):
                runner(root, root / "results", self.config)


if __name__ == "__main__":
    unittest.main()

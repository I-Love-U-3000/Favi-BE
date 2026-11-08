import random
import unittest
from community_policy import population, initial_edges, activation, observe, decide, gini


class CommunityPolicyTests(unittest.TestCase):
    def test_population_reproducible_and_skewed(self):
        people = population(42)
        self.assertEqual([p.record() for p in people], [p.record() for p in population(42)])
        self.assertEqual([sum(p.role == r for p in people) for r in ("reader", "contributor", "creator")], [90, 9, 1])
        self.assertNotEqual(people[0].record(), population(43)[0].record())

    def test_initial_graph_has_no_self_or_duplicate_edges(self):
        edges = initial_edges(population(42), 42)
        self.assertEqual(len(edges), 200)
        self.assertEqual(len(set(edges)), 200)
        self.assertTrue(all(a != b for a, b in edges))

    def test_fatigue_reduces_activity(self):
        person = population(42)[0]
        original = activation(person)
        person.fatigue = .8
        self.assertAlmostEqual(activation(person), original * .2)

    def test_opinion_only_changes_inside_confidence_bound(self):
        person = population(42)[0]
        person.opinion[0] = .2
        person.openness = .15
        self.assertEqual(observe(person, 0, .9), 0)
        self.assertGreater(observe(person, 0, .3), 0)
        self.assertLess(person.opinion[0], .3)

    def test_reader_cannot_write_or_repeat_like(self):
        person = population(42)[0]
        person.role = "reader"
        person.liked.add(1)
        person.follows.add(99)
        for seed in range(100):
            self.assertEqual(decide(person, [{"id": 1, "author": 99, "topic": 0}], random.Random(seed))[0], "read")

    def test_gini_known_cases(self):
        self.assertEqual(gini([1, 1, 1]), 0)
        self.assertEqual(gini([0, 0, 0]), 0)
        self.assertAlmostEqual(gini([0, 0, 3]), 2 / 3)

    def test_creator_can_publish_when_only_own_posts_exist(self):
        person = next(p for p in population(42) if p.role == "creator")
        self.assertEqual(decide(person, [], random.Random(1)), ("post", None))

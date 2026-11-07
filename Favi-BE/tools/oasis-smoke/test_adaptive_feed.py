import unittest
from adaptive_feed import AdaptiveFeed


class AdaptiveFeedTests(unittest.TestCase):
    def setUp(self):
        self.rows = [dict(user_id=u, arrival_tick=-1, preference_tick=-1, topics=['food']) for u in range(4)]
        self.events = [dict(kind='post', user_id=1, post_id=1, topic=1, tick=-1),
                       dict(kind='post', user_id=2, post_id=2, topic=9, tick=-1),
                       dict(kind='post', user_id=0, post_id=3, topic=1, tick=-1),
                       dict(kind='post', user_id=1, post_id=4, topic=1, tick=10),
                       dict(kind='like', user_id=3, post_id=2, tick=0)]

    def test_topic_priority_general_fallback_and_common_eligibility(self):
        feed = AdaptiveFeed(self.events, 10, self.rows)
        self.assertEqual(feed.rank(0, 10), [1, 2])
        self.assertEqual(feed.candidates(0), {1, 2})
        pop = AdaptiveFeed(self.events, 10, self.rows, method='popularity')
        self.assertEqual(pop.rank(0, 10), [2, 1])
        self.assertEqual(feed.candidates(0), pop.candidates(0))

    def test_new_arrival_and_future_preference(self):
        self.rows[0]['arrival_tick'] = 10
        self.rows[0]['preference_tick'] = 10
        self.assertEqual(AdaptiveFeed(self.events, 10, self.rows).rank(0, 10), [])
        self.rows[0]['arrival_tick'] = -1
        feed = AdaptiveFeed(self.events, 10, self.rows)
        self.assertNotEqual(feed.topics(0), ['food'])

    def test_prior_positive_and_threshold(self):
        self.events.append(dict(kind='like', user_id=0, post_id=2, tick=1))
        feed = AdaptiveFeed(self.events, 10, self.rows)
        self.assertEqual(feed.rank(0, 10), [1])
        self.assertEqual(AdaptiveFeed(self.events, 10, self.rows, threshold=5).rank(0, 10), [1])
        with self.assertRaises(ValueError):
            AdaptiveFeed(self.events, 10, self.rows, method='collaborative')

    def test_bounded_general_pool_and_tagged_cohort(self):
        events = [dict(kind='post', user_id=1, post_id=i, topic=9, tick=-1) for i in range(1, 81)]
        feed = AdaptiveFeed(events, 10, self.rows)
        self.assertEqual(len(feed.candidates(0)), 80)
        self.assertEqual(feed.rank(0, 300), list(range(1, 61)))
        self.assertEqual(feed.cohort(0), 'C3')
        self.rows[0]['topics'] = []
        self.assertEqual(AdaptiveFeed(events, 10, self.rows).cohort(0), 'C0')

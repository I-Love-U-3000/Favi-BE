import random
import sqlite3
import unittest
from community_policy import population, initial_edges
from dataset_policy import sample_feed
from dataset_fast import FastFeed, initial_edges_fast, install_indexes


class DatasetFastTests(unittest.TestCase):
    def test_bootstrap_matches_scalar_rule(self):
        for size, seed in ((100, 42), (300, 20261104), (1000, 20261105)):
            people = population(seed, size, 8)
            self.assertEqual(initial_edges(people, seed), initial_edges_fast(people, seed))

    def test_feed_matches_scalar_choices_and_rng_state(self):
        people = population(42, 100, 8)
        posts = [{"id": i + 1, "author": i % 100, "topic": i % 8} for i in range(500)]
        sampler = FastFeed(posts)
        for exploration in (0, .5, 1):
            for seed in range(50):
                person = people[seed]
                person.follows.update({(seed + 1) % 100, (seed + 5) % 100})
                person.seen.update(range(seed, seed + 20))
                a, b = random.Random(seed), random.Random(seed)
                config = {"feed_size": 5, "exploration": exploration}
                self.assertEqual(sample_feed(person, posts, people, config, a), sampler.sample(person, config, b))
                self.assertEqual(a.getstate(), b.getstate())
            posts.append({"id": len(posts) + 1, "author": 99, "topic": 0})

    def test_index_overlay_does_not_add_constraints_or_rows(self):
        with sqlite3.connect(":memory:") as db:
            for sql in ('CREATE TABLE "like"(post_id INTEGER,user_id INTEGER)',
                        'CREATE TABLE follow(follower_id INTEGER,followee_id INTEGER)',
                        'CREATE TABLE comment(post_id INTEGER)', 'CREATE TABLE user(agent_id INTEGER)'):
                db.execute(sql)
            before = db.execute("SELECT name,sql FROM sqlite_master WHERE type='table' ORDER BY name").fetchall()
            metadata = install_indexes(db)
            self.assertEqual(before, db.execute("SELECT name,sql FROM sqlite_master WHERE type='table' ORDER BY name").fetchall())
            self.assertEqual(len(metadata["nonunique_index_sql"]), 5)
            self.assertTrue(all(row[2] == 0 for row in db.execute('PRAGMA index_list("like")')))


if __name__ == "__main__":
    unittest.main()

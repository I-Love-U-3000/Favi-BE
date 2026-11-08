"""Vectorized execution of the existing seeded sampling rules, not a new policy."""
import random
import numpy as np


def weighted_index(weights, rng):
    cumulative = np.cumsum(weights, dtype=np.float64)
    return min(int(np.searchsorted(cumulative, rng.random() * float(cumulative[-1]), side="right")), len(weights) - 1)


def initial_edges_fast(people, seed):
    rng = random.Random(seed + 1)
    preferences = np.array([p.interests for p in people], dtype=np.float64)
    ids = np.array([p.id for p in people], dtype=np.int64)
    edges = []
    for person in people:
        distance = np.zeros(len(people), dtype=np.float64)
        for topic, interest in enumerate(person.interests):
            distance += np.abs(preferences[:, topic] - interest)
        affinity = 1 - distance / 2
        weights = np.fromiter((.1 + float(value) ** 3 for value in affinity), dtype=np.float64)
        available = ids != person.id
        candidates, weights = ids[available], weights[available]
        for _ in range(2):
            index = weighted_index(weights, rng)
            edges.append((person.id, int(candidates[index])))
            candidates, weights = np.delete(candidates, index), np.delete(weights, index)
    return edges


class FastFeed:
    def __init__(self, posts):
        self.posts, self.count = posts, -1

    def sample(self, person, config, rng):
        if self.count != len(self.posts):
            self.ids = np.array([p["id"] for p in self.posts], dtype=np.int64)
            self.authors = np.array([p["author"] for p in self.posts], dtype=np.int64)
            self.topics = np.array([p["topic"] for p in self.posts], dtype=np.int64)
            self.count = len(self.posts)
        available = np.flatnonzero(self.authors != person.id)
        selected, weights = [], None
        for _ in range(min(config["feed_size"], len(available))):
            if rng.random() < config["exploration"]:
                index = rng.randrange(len(available))
            else:
                if weights is None:
                    weights = .05 + np.array(person.interests)[self.topics[available]]
                    weights *= np.where(np.isin(self.authors[available], np.fromiter(person.follows, dtype=np.int64)), 1.3, 1)
                    weights *= np.where(np.isin(self.ids[available], np.fromiter(person.seen, dtype=np.int64)), .2, 1)
                index = weighted_index(weights, rng)
            selected.append(self.posts[int(available[index])])
            available = np.delete(available, index)
            if weights is not None:
                weights = np.delete(weights, index)
        return selected


INDEX_SQL = (
    'CREATE INDEX dataset_like_post_user ON "like"(post_id,user_id)',
    'CREATE INDEX dataset_follow_pair ON follow(follower_id,followee_id)',
    'CREATE INDEX dataset_follow_incoming ON follow(followee_id)',
    'CREATE INDEX dataset_comment_post ON comment(post_id)',
    'CREATE INDEX dataset_user_agent ON user(agent_id)',
)


def install_indexes(database):
    for statement in INDEX_SQL:
        database.execute(statement)
    database.commit()
    plan = database.execute('EXPLAIN QUERY PLAN SELECT * FROM "like" WHERE post_id=? AND user_id=?', (1, 1)).fetchall()
    if not any("dataset_like_post_user" in str(row) for row in plan):
        raise ValueError("Expected source lookup index is not active")
    return {"nonunique_index_sql": list(INDEX_SQL), "like_lookup_query_plan": [list(row) for row in plan],
            "scope": "Indexes only; no new tables, rows, uniqueness constraints or upstream source patches"}

"""Seeded, inspectable behavioral assumptions; not a calibrated human model."""
from dataclasses import dataclass, field, asdict
import math
import random

TOPICS = ("food", "transport", "photography")
LENSES = ("cost and accessibility", "ingredients and preparation", "local customs",
          "crowding and quiet hours", "safety and practical details", "environmental impact")


@dataclass
class Person:
    id: int
    role: str
    interests: list
    opinion: list
    openness: float
    learning: float
    sociability: float
    lens: str
    style: str
    fatigue: float = 0.0
    seen: set = field(default_factory=set)
    liked: set = field(default_factory=set)
    commented: set = field(default_factory=set)
    follows: set = field(default_factory=set)
    trust: dict = field(default_factory=dict)

    def record(self):
        data = asdict(self)
        for key in ("seen", "liked", "commented", "follows"):
            data[key] = sorted(data[key])
        return data


def population(seed, size=100, topic_count=3):
    if size < 100 or size % 100:
        raise ValueError("Population must be a multiple of 100 for exact 90/9/1 role counts")
    rng = random.Random(seed)
    roles = ["reader"] * (size * 90 // 100) + ["contributor"] * (size * 9 // 100) + ["creator"] * (size // 100)
    rng.shuffle(roles)
    people = []
    for index, role in enumerate(roles):
        weights = [rng.gammavariate(0.6, 1) for _ in range(topic_count)]
        total = sum(weights)
        people.append(Person(index, role, [w / total for w in weights],
                             [rng.random() for _ in range(topic_count)], rng.uniform(.15, .45),
                             rng.uniform(.05, .2), rng.uniform(.2, .9),
                             rng.choice(LENSES), rng.choice(("a concise question", "a practical suggestion", "a cautious counterpoint"))))
    return people


def affinity(a, b):
    return 1 - sum(abs(x - y) for x, y in zip(a.interests, b.interests)) / 2


def initial_edges(people, seed):
    rng = random.Random(seed + 1)
    edges = []
    for person in people:
        candidates = [other for other in people if other.id != person.id]
        # Two unique contacts each: homophily plus baseline exploration, not cliques.
        for _ in range(2):
            other = rng.choices(candidates, weights=[.1 + affinity(person, x) ** 3 for x in candidates])[0]
            candidates.remove(other)
            edges.append((person.id, other.id))
    return edges


def activation(person):
    baseline = {"reader": .12, "contributor": .65, "creator": .9}[person.role]
    return baseline * (1 - person.fatigue)


def observe(person, topic, expressed_opinion):
    before = person.opinion[topic]
    if abs(before - expressed_opinion) <= person.openness:
        person.opinion[topic] += person.learning * (expressed_opinion - before)
    return person.opinion[topic] - before


def rank_feed(person, posts, people):
    def score(post):
        author = people[post["author"]]
        social = math.log1p(post["likes"]) / (1 + math.log1p(post["likes"]))
        return (.5 * person.interests[post["topic"]] + .15 * affinity(person, author)
                + .15 * person.trust.get(author.id, .3) + .1 * social
                + .1 * (post["id"] not in person.seen))
    return sorted((p for p in posts if p["author"] != person.id),
                  key=lambda p: (-score(p), p["id"]))[:3]


def decide(person, feed, rng):
    """At most one mutation per activation; most readers consume silently."""
    if person.role == "creator" and rng.random() < .15:
        return "post", None
    if not feed:
        return "read", None
    post = feed[0]
    if person.role != "reader" and post["id"] not in person.commented and rng.random() < .35:
        return "comment", post
    if post["author"] not in person.follows and rng.random() < .12 * person.sociability:
        return "follow", post
    if post["id"] not in person.liked and rng.random() < .2 + .35 * person.interests[post["topic"]]:
        return "like", post
    return "read", post


def gini(values):
    values = sorted(values)
    total = sum(values)
    return 0.0 if not total else sum((2 * i - len(values) - 1) * x for i, x in enumerate(values, 1)) / (len(values) * total)

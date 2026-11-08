"""Versioned synthetic workload policy and fail-closed temporal data validation."""
from collections import Counter
import hashlib
import json
import math
import random


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")


def fingerprint(value):
    return hashlib.sha256(canonical(value)).hexdigest()


def validate_config(config):
    keys = {"schema_version", "seed", "users", "initial_posts", "max_posts", "ticks", "feed_size", "exploration", "topics", "activity", "quality"}
    if not isinstance(config, dict) or set(config) != keys or type(config["schema_version"]) is not int or config["schema_version"] != 1:
        raise ValueError("Expected exactly the version1 configuration keys")
    for key, low, high in (("seed", 0, 2147483647), ("users", 100, 10000), ("initial_posts", 10, 10000),
                           ("max_posts", 10, 20000), ("ticks", 3, 500), ("feed_size", 1, 20)):
        if type(config[key]) is not int or not low <= config[key] <= high:
            raise ValueError(f"{key} must be an integer in [{low},{high}]")
    if config["users"] % 100 or config["initial_posts"] > config["max_posts"]:
        raise ValueError("Use multiples of100 users and initial_posts <= max_posts")
    topics = config["topics"]
    if not isinstance(topics, list) or not 2 <= len(topics) <= 32 or any(not isinstance(t, str) or not t.strip() or len(t) > 60 for t in topics) or len(set(topics)) != len(topics):
        raise ValueError("topics must be2..32 unique nonempty names within60characters")
    def probability(value):
        return type(value) in (int, float) and math.isfinite(value) and 0 <= value <= 1
    if not probability(config["exploration"]):
        raise ValueError("exploration must be a finite probability")
    if not isinstance(config["activity"], dict) or set(config["activity"]) != {"reader", "contributor", "creator"} or not all(probability(v) for v in config["activity"].values()):
        raise ValueError("activity must specify all three role probabilities")
    quality = config["quality"]
    if not isinstance(quality, dict) or set(quality) != {"min_likes", "min_warm_users", "warm_min_likes"} or any(type(v) is not int or v < 1 for v in quality.values()):
        raise ValueError("quality must specify three positive integer thresholds")
    if quality["min_warm_users"] > config["users"] or quality["warm_min_likes"] > config["max_posts"]:
        raise ValueError("Impossible warm-user threshold")
    return config


def sample_feed(person, posts, people, config, rng):
    pool = [p for p in posts if p["author"] != person.id]
    visible = []
    for _ in range(min(config["feed_size"], len(pool))):
        if rng.random() < config["exploration"]:
            post = rng.choice(pool)
        else:
            weights = [(.05 + person.interests[p["topic"]]) * (1.3 if p["author"] in person.follows else 1)
                       * (1 if p["id"] not in person.seen else .2) for p in pool]
            post = rng.choices(pool, weights=weights)[0]
        pool.remove(post)
        visible.append(post)
    return visible


def choose_action(person, feed, config, rng, post_count):
    if person.role == "creator" and post_count < config["max_posts"] and rng.random() < .15:
        return "post", None
    if not feed:
        return "read", None
    post = rng.choices(feed, weights=[.05 + person.interests[p["topic"]] for p in feed])[0]
    if person.role != "reader" and post["id"] not in person.commented and rng.random() < .15:
        return "comment", post
    if post["author"] not in person.follows and rng.random() < .08 * person.sociability:
        return "follow", post
    # Exposure does not imply a positive. Repeated likes are never emitted.
    if post["id"] not in person.liked and rng.random() < .15 + .65 * person.interests[post["topic"]]:
        return "like", post
    return "read", post


def content(kind, topic, actor, ordinal):
    # Explicit synthetic corpus, not a claim that a person said this naturally.
    if kind == "post":
        return f"Synthetic community note {ordinal}: member {actor} shares a {topic} idea for discussion."
    return f"Synthetic reply {ordinal}: member {actor} asks about practical tradeoffs in this {topic} idea."


def audit_events(events, config):
    """Validate sequence as recorded, not a reconstruction from final state."""
    items, likes, follows, exposures = {}, set(), set(), {}
    last_tick = -1
    for expected, event in enumerate(events):
        if type(event["seq"]) is not int or event["seq"] != expected or type(event["tick"]) is not int or not -1 <= event["tick"] < config["ticks"] or event["tick"] < last_tick:
            raise ValueError("Non-monotonic event order/tick")
        last_tick = event["tick"]
        user = event["user_id"]
        if type(user) is not int or not 0 <= user < config["users"] or type(event["bootstrap"]) is not bool or event["bootstrap"] != (event["tick"] == -1):
            raise ValueError("Invalid actor/bootstrap")
        kind = event["kind"]
        if kind == "post":
            item = event["post_id"]
            if type(item) is not int or item <= 0 or item in items or type(event["topic"]) is not int or not 0 <= event["topic"] < len(config["topics"]):
                raise ValueError("Invalid or duplicate item")
            items[item] = user
        elif kind == "follow":
            other = event["target_user_id"]
            if type(other) is not int or not 0 <= other < config["users"] or user == other or (user, other) in follows:
                raise ValueError("Invalid or duplicate follow")
            follows.add((user, other))
        elif kind == "exposure":
            ids = event["post_ids"]
            if event["bootstrap"] or not isinstance(ids, list) or not 1 <= len(ids) <= config["feed_size"] or len(set(ids)) != len(ids) or any(type(i) is not int or i not in items or items[i] == user for i in ids):
                raise ValueError("Exposure contains unavailable/self/duplicate items")
            exposures[event["seq"]] = event
        elif kind in {"like", "comment"}:
            item = event["post_id"]
            exposure = exposures.get(event["exposure_seq"])
            if type(item) is not int or type(event["exposure_seq"]) is not int or item not in items or items[item] == user or exposure is None or exposure["user_id"] != user or exposure["tick"] != event["tick"] or item not in exposure["post_ids"]:
                raise ValueError("Interaction has no preceding valid exposure")
            if kind == "like":
                if (user, item) in likes:
                    raise ValueError("Repeated positive edge")
                likes.add((user, item))
        else:
            raise ValueError(f"Unknown event kind: {kind}")
    counts = Counter(u for u, _ in likes)
    warm = sum(n >= config["quality"]["warm_min_likes"] for n in counts.values())
    return {"events": len(events), "posts": len(items), "likes": len(likes), "follows": len(follows),
            "exposures": len(exposures), "warm_users": warm,
            "quality_passed": config["initial_posts"] <= len(items) <= config["max_posts"] and len(likes) >= config["quality"]["min_likes"] and warm >= config["quality"]["min_warm_users"]}

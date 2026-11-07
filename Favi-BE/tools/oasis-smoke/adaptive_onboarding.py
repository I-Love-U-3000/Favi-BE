"""Deterministic arrival/preference plan; deliberately separate from native actions."""
import random

TOPICS = ("travel", "food", "sports", "city", "nature", "technology", "lifestyle", "art", "fitness", "music")


def plan(users, seed, ticks=500, arrival_policy="uniform"):
    if type(users) is not int or users < 100 or type(seed) is not int or type(ticks) is not int or ticks < 10:
        raise ValueError("Expected >=100 users, integer seed and >=10 ticks")
    rng = random.Random(seed)
    if arrival_policy not in ("uniform", "cutoff-waves-v1", "lifecycle-v2"):
        raise ValueError("Unknown arrival policy")
    rows = []
    for user in range(users):
        # Bootstrap creators remain available; later arrivals give sparse histories.
        arrival = -1 if user < users // 2 else rng.randrange(ticks)
        count = rng.choice((0, 1, 3))
        if arrival_policy == "cutoff-waves-v1":
            # Controlled stress population, assigned before observing any labels.
            group = user * 10 // users
            arrival = -1 if group < 4 else ticks * 3 // 5 - 1 if group < 7 else ticks * 4 // 5 - 1
            count = (0, 1, 3)[user % 3]
        if arrival_policy == "lifecycle-v2":
            fraction = user / users
            if fraction < .3:
                arrival = -1
            elif fraction < .45:
                arrival = ticks * 3 // 5 - 1
            elif fraction < .6:
                arrival = ticks * 4 // 5 - 1
            else:
                cutoff = ticks * (3 if fraction < .8 else 4) // 5
                arrival = max(0, cutoff - (10, 30, 60)[user % 3])
            count = (0, 1, 3)[(user // 3) % 3]
        rows.append({"user_id": user, "arrival_tick": arrival,
                     "preference_tick": arrival, "topics": rng.sample(list(TOPICS), count)})
    validate(rows, users, ticks)
    return rows


def preferences(rows, people, mode):
    """Alternative declared metadata, independent of future feedback."""
    if mode not in ('informative', 'noisy', 'independent'):
        raise ValueError('Unknown preference mode')
    import math
    result = []
    for row, person in zip(rows, people, strict=True):
        if row['user_id'] != person.id:
            raise ValueError('Population order mismatch')
        count = len(row['topics'])
        top = sorted(range(len(TOPICS)), key=lambda i: (-person.interests[i], i))[:count]
        selected = [TOPICS[i] for i in top]
        if mode == 'independent':
            selected = list(row['topics'])
        elif mode == 'noisy' and count:
            replace = math.ceil(count / 2)
            outside = [t for t in TOPICS if t not in selected]
            selected = selected[:-replace] + outside[:replace]
        result.append({**row, 'topics': selected})
    return result


def validate(rows, users, ticks):
    if len(rows) != users or {r["user_id"] for r in rows} != set(range(users)):
        raise ValueError("Missing or duplicate user")
    for row in rows:
        if type(row["user_id"]) is not int or type(row["arrival_tick"]) is not int or type(row["preference_tick"]) is not int:
            raise ValueError("IDs and timestamps must be integers")
        if not -1 <= row["arrival_tick"] <= row["preference_tick"] < ticks:
            raise ValueError("Invalid arrival/preference timeline")
        topics = row["topics"]
        if not isinstance(topics, list) or len(topics) > 10 or len(topics) != len(set(topics)) or not set(topics) <= set(TOPICS):
            raise ValueError("Invalid declared topics")


def visible_preferences(row, cutoff):
    if row["arrival_tick"] >= cutoff:
        return None
    return list(row["topics"]) if row["preference_tick"] < cutoff else []


def classify_cohort(positive_count, declared_topics, matching_items):
    if positive_count:
        return 'T1' if positive_count == 1 else 'T2' if positive_count < 5 else 'W1' if positive_count < 20 else 'W2'
    if not declared_topics:
        return 'C0'
    return 'C3' if matching_items < 10 else 'C1' if len(declared_topics) == 1 else 'C2'

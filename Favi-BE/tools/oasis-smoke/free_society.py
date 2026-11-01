"""Technical boundaries only; no activity quotas, assigned personas or emotion rules."""
import copy
import json
KINDS = ("wait", "read", "post", "comment", "like", "follow", "unfollow", "unlike", "event", "join_event", "leave_event")
PROFILE_FIELDS = {"name": 60, "persona": 4096, "public_bio": 4096, "emotion": 4096, "goal": 4096}


def object_schema(properties):
    return {"type": "object", "properties": properties, "required": list(properties), "additionalProperties": False}


PROFILE_SCHEMA = object_schema({key: {"type": "string", "minLength": 1} for key in PROFILE_FIELDS})
TURN_SCHEMA = object_schema({
    "persona": {"type": "string"}, "emotion": {"type": "string"}, "goal": {"type": "string"},
    "personal_event": {"type": "string"}, "action": {"type": "string", "enum": list(KINDS)},
    "target": {"type": ["integer", "null"]}, "content": {"type": "string"},
    "sleep_ticks": {"type": "integer", "minimum": 0, "maximum": 3}})
for _key in ("persona", "emotion", "goal", "personal_event", "content"):
    TURN_SCHEMA["properties"][_key]["minLength"] = 1 if _key in {"persona", "emotion", "goal"} else 0


def decision_schema(own_id, people, posts, liked, followed, events, joined):
    """All currently legal actions, with model-selected IDs, no policy weights."""
    targets = {"wait": None, "read": None, "post": None, "event": None,
               "comment": sorted(posts), "like": sorted(posts - liked), "unlike": sorted(liked),
               "follow": sorted(people - followed - {own_id}), "unfollow": sorted(followed),
               "join_event": sorted(events - joined), "leave_event": sorted(joined)}
    branches = []
    for kind, choices in targets.items():
        if choices == []:
            continue
        branch = copy.deepcopy(TURN_SCHEMA)
        branch["properties"]["action"] = {"const": kind}
        branch["properties"]["target"] = {"type": "null"} if choices is None else {"type": "integer", "enum": choices}
        if kind in {"post", "comment", "event"}:
            branch["properties"]["content"]["minLength"] = 1
        branches.append(branch)
    return {"anyOf": branches}


def bounded_context(context, max_bytes=8000):
    """Trim observations, never the canonical private or shared world state."""
    result = copy.deepcopy(context)
    while len(json.dumps(result, ensure_ascii=False).encode("utf-8")) > max_bytes:
        memories = result.get("your_private_state", {}).get("memory", [])
        feed = result.get("visible_posts", [])
        events = result.get("public_events", [])
        with_comments = next((post for post in reversed(feed) if post.get("comments")), None)
        if memories:
            memories.pop(0)
        elif with_comments is not None:
            with_comments["comments"].pop()
        elif len(feed) > 1:
            feed.pop()
        elif events:
            events.pop(0)
        else:
            raise ValueError("Mandatory context exceeds byte budget")
    return result


def validate_profile(profile):
    if not isinstance(profile, dict) or set(profile) != set(PROFILE_FIELDS):
        raise ValueError("Invalid profile fields")
    for key, bound in PROFILE_FIELDS.items():
        if not isinstance(profile[key], str) or not profile[key].strip() or len(profile[key]) > bound:
            raise ValueError(f"Invalid {key}")
    return dict(profile)


def validate_turn(turn, own_id, known_people, known_posts, liked, followed, known_events=None, joined=None):
    if not isinstance(turn, dict) or set(turn) != set(TURN_SCHEMA["properties"]):
        raise ValueError("Invalid turn fields")
    for key, bound in {"persona": 4096, "emotion": 4096, "goal": 4096, "personal_event": 4096, "content": 1000}.items():
        if not isinstance(turn[key], str) or len(turn[key]) > bound:
            raise ValueError(f"Invalid {key}")
    for key in ("persona", "emotion", "goal"):
        if not turn[key].strip():
            raise ValueError(f"Empty {key}")
    if type(turn["sleep_ticks"]) is not int or not 0 <= turn["sleep_ticks"] <= 3:
        raise ValueError("Invalid sleep interval")
    kind, target = turn["action"], turn["target"]
    if kind not in KINDS:
        raise ValueError("Unsupported action")
    if kind in {"comment", "like", "unlike"}:
        if type(target) is not int or target not in known_posts:
            raise ValueError("Unknown post")
    elif kind in {"follow", "unfollow"}:
        if type(target) is not int or target not in known_people or target == own_id:
            raise ValueError("Unknown person or self-follow")
    elif kind in {"join_event", "leave_event"}:
        if type(target) is not int or target not in (known_events or set()):
            raise ValueError("Unknown public event")
        if kind == "join_event" and target in (joined or set()) or kind == "leave_event" and target not in (joined or set()):
            raise ValueError("Event membership already in requested state")
    elif target is not None:
        raise ValueError("This action has no target")
    if kind in {"post", "comment", "event"} and not turn["content"].strip():
        raise ValueError("Empty public content")
    if kind == "like" and target in liked or kind == "unlike" and target not in liked:
        raise ValueError("Reaction already in requested state")
    if kind == "follow" and target in followed or kind == "unfollow" and target not in followed:
        raise ValueError("Follow already in requested state")
    return dict(turn)


def apply_event(kind, agent_id, target, content, post_id, tick, events, joined):
    """A claim about another person never changes that person's membership."""
    if kind == "event":
        event = {"id": len(events) + 1, "owner": agent_id, "description": content,
                 "post_id": post_id, "created_tick": tick, "members": []}
        events.append(event)
        # Publishing an invitation does not mean the organizer chose to attend.
        return
    event = next(e for e in events if e["id"] == target)
    if kind == "join_event":
        if agent_id in event["members"] or target in joined:
            raise ValueError("Already joined")
        event["members"].append(agent_id)
        joined.append(target)
    elif kind == "leave_event":
        if agent_id not in event["members"] or target not in joined:
            raise ValueError("Not joined")
        event["members"].remove(agent_id)
        joined.remove(target)
    else:
        raise ValueError("Not an event action")

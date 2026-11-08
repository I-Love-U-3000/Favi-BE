"""Hybrid social ABM: seeded policy decisions + bounded local LLM content.

OASIS owns all durable social records. This is explicitly not LLMAction autonomy.
"""
import asyncio
from collections import Counter
from difflib import SequenceMatcher
import hashlib
import importlib.metadata
import importlib.util
import json
import os
from pathlib import Path
import random
import re
import sqlite3
import sys
from datetime import datetime, timezone
from urllib.parse import urlparse

from community_policy import TOPICS, population, initial_edges, activation, observe, rank_feed, decide, gini
from source_provenance import verify_runtime_source, verify_dependency_lock

PINNED = "ba892e0667eb84b312e4c0e5630f275154bbb98f"
MODEL = "qwen3:1.7b"
SEED = 20261031
TICKS = 20
MAX_CALLS = 12
SCENARIOS = (
    "A neighborhood weekend market wants affordable meals with clearly listed ingredients.",
    "Residents are comparing bus fares with cycling for a short weekend trip.",
    "A busy local market is discussing quiet visiting hours and permission before photographing vendors.",
)


def normalized(text):
    return re.sub(r"\W+", " ", text.casefold()).strip()


async def run(root, upstream, url):
    parsed = urlparse(url)
    if parsed.scheme != "http" or parsed.hostname not in {"127.0.0.1", "localhost", "::1"} or parsed.username or parsed.password:
        raise ValueError("Use a credential-free HTTP loopback Ollama endpoint")
    spec = importlib.util.find_spec("oasis")
    source = verify_runtime_source(upstream, Path(spec.origin).parent, PINNED)
    locked = verify_dependency_lock(Path(__file__).with_name("requirements-oasis.lock.txt"))
    if importlib.metadata.version("camel-oasis") != "0.2.5":
        raise RuntimeError("Unexpected OASIS distribution")
    os.chdir(root)
    (root / "log").mkdir()
    import httpx
    import oasis
    from camel.models import StubModel
    from camel.types import ModelType
    from oasis import AgentGraph, SocialAgent, UserInfo, ActionType
    from oasis.clock.clock import Clock
    from oasis.social_platform.platform import Platform
    if Path(oasis.__file__).resolve().parent != Path(spec.origin).resolve().parent:
        raise RuntimeError("OASIS import resolution changed")

    people = population(SEED)
    initial = [p.record() for p in people]
    rng = random.Random(SEED + 2)
    graph = AgentGraph()
    model = StubModel(model_type=ModelType.STUB)  # Only a constructor dependency; never used for inference.
    agents = []
    for p in people:
        agent = SocialAgent(agent_id=p.id, user_info=UserInfo(user_name=f"community_{p.id}",
            name=f"Member {p.id:03d}", description=f"{p.role}; {p.lens}; {p.style}",
            profile=None, recsys_type="reddit"), agent_graph=graph, model=model,
            available_actions=[ActionType.CREATE_POST, ActionType.CREATE_COMMENT, ActionType.LIKE_POST, ActionType.FOLLOW])
        graph.add_agent(agent)
        agents.append(agent)
    database = root / "oasis.db"
    os.environ["OASIS_DB_PATH"] = str(database)
    platform = Platform(db_path=str(database), recsys_type="reddit", sandbox_clock=Clock(k=1),
                        start_time=datetime.now(timezone.utc).replace(tzinfo=None))
    env = oasis.make(agent_graph=graph, platform=platform, database_path=str(database))
    calls, events, rejected, posts = [], [], [], []
    accepted_content = []
    accepted_topics = []
    mutations = Counter()
    exposures = Counter()
    updates = []
    client = httpx.AsyncClient(timeout=90, follow_redirects=False)
    await env.reset()

    def rows(sql, parameters=()):
        with sqlite3.connect(f"file:{database.as_posix()}?mode=ro", uri=True) as db:
            return db.execute(sql, parameters).fetchall()

    async def execute(person, action, arguments, tick, bootstrap=False):
        # Public OASIS action API, sequential and awaited. All writes belong to OASIS.
        result = await agents[person.id].perform_action_by_data(action, **arguments)
        events.append({"tick": tick, "agent": person.id, "action": action.value,
                       "arguments": arguments, "result": result, "bootstrap": bootstrap})
        if not isinstance(result, dict) or not result.get("success"):
            raise RuntimeError(f"OASIS rejected action: {events[-1]}")
        mutations[person.id] += 1

    async def content(person, kind, topic, post=None):
        if len(calls) >= MAX_CALLS:
            events.append({"agent": person.id, "content_skip": "LLM budget exhausted", "kind": kind})
            return None
        recent = [text for text, accepted_topic in zip(accepted_content, accepted_topics) if accepted_topic == topic][-6:]
        context = {"topic": TOPICS[topic], "scenario": SCENARIOS[topic],
                   "post": post["content"] if post else None,
                   "recent_content_do_not_repeat": recent}
        messages = [{"role": "system", "content":
            f"You are community member {person.id}, focused on {person.lens}. "
            f"Write {person.style}. Your view on this topic is {person.opinion[topic]:.2f} "
            "on a scale from skeptical 0 to enthusiastic 1. "
            "Return JSON with one content field, at most 30 words. Be specific to your focus. "
            "Do not copy existing content, invent personal visits, or claim unprovided facts. "
            "If commenting, contribute a new question or point."},
            {"role": "user", "content": f"Write one {kind}. Context: " + json.dumps(context)}]
        response = await client.post(url + "/api/chat", json={"model": MODEL, "messages": messages,
            "stream": False, "think": False, "format": {"type": "object", "properties":
            {"content": {"type": "string"}}, "required": ["content"], "additionalProperties": False},
            "options": {"temperature": .8, "num_predict": 128, "num_ctx": 4096,
                        "seed": SEED + len(calls)}})
        response.raise_for_status()
        data = response.json()
        calls.append({"agent": person.id, "kind": kind, "messages": messages, "response": data})
        (root / "llm-calls.json").write_text(json.dumps(calls, ensure_ascii=False, indent=2), encoding="utf-8")
        try:
            text = json.loads(data["message"]["content"])["content"]
            if not isinstance(text, str):
                raise ValueError("content must be a string")
            text = text.strip()
            if not data.get("done") or data.get("done_reason") == "length" or not text or len(text.split()) > 45:
                raise ValueError("incomplete/empty/too long")
            if any(SequenceMatcher(None, normalized(text), normalized(old)).ratio() >= .85 for old in accepted_content):
                raise ValueError("duplicate or near-duplicate text")
        except (ValueError, KeyError, TypeError) as error:
            rejected.append({"call": len(calls), "reason": str(error)})
            return None
        accepted_content.append(text)
        accepted_topics.append(topic)
        return text

    try:
        identity = rows('SELECT user_id, agent_id FROM user ORDER BY user_id')
        if identity != [(p.id, p.id) for p in people]:
            raise RuntimeError("Pinned engine no longer maps simulation agent IDs to these user IDs")
        version_response = await client.get(url + "/api/version")
        version_response.raise_for_status()
        tags_response = await client.get(url + "/api/tags")
        tags_response.raise_for_status()
        model_info = next(x for x in tags_response.json()["models"] if x["name"] == MODEL)
        # Synthetic founding graph, explicitly separate from later endogenous follows.
        for a, b in initial_edges(people, SEED):
            await execute(people[a], ActionType.FOLLOW, {"followee_id": b}, -1, True)
            people[a].follows.add(b)
            people[a].trust[b] = .45
        founder = next(p for p in people if p.role == "creator")
        # Three opening topics are an explicit scenario setup, not an emergent result.
        async def publish(person, topic, tick, bootstrap=False):
            text = await content(person, "post", topic)
            if text is None:
                return
            await execute(person, ActionType.CREATE_POST, {"content": text}, tick, bootstrap)
            post_id = rows('SELECT MAX(post_id) FROM post')[0][0]
            posts.append({"id": post_id, "author": person.id, "topic": topic,
                          "opinion": person.opinion[topic], "content": text, "likes": 0})
        for topic in range(3):
            await publish(founder, topic, -1, True)
        if not posts:
            raise RuntimeError("No valid opening content; abort instead of fabricating posts")
        for tick in range(TICKS):
            order = people.copy()
            rng.shuffle(order)
            for person in order:
                person.fatigue *= .75
                if rng.random() >= activation(person):
                    continue
                feed = rank_feed(person, posts, people)
                for post in feed:
                    exposures[person.id] += 1
                    delta = observe(person, post["topic"], post["opinion"])
                    updates.append({"tick": tick, "agent": person.id, "post": post["id"], "delta": delta})
                    person.seen.add(post["id"])
                kind, post = decide(person, feed, rng)
                interacted = False
                events.append({"tick": tick, "agent": person.id, "decision": kind,
                               "visible_post_ids": [p["id"] for p in feed]})
                if kind == "post" and len(posts) < 5:
                    await publish(person, max(range(3), key=lambda i: person.interests[i]), tick)
                elif kind == "comment":
                    text = await content(person, "comment", post["topic"], post)
                    if text is not None:
                        await execute(person, ActionType.CREATE_COMMENT, {"post_id": post["id"], "content": text}, tick)
                        person.commented.add(post["id"])
                        interacted = True
                elif kind == "like":
                    await execute(person, ActionType.LIKE_POST, {"post_id": post["id"]}, tick)
                    person.liked.add(post["id"])
                    post["likes"] += 1
                    interacted = True
                elif kind == "follow":
                    await execute(person, ActionType.FOLLOW, {"followee_id": post["author"]}, tick)
                    person.follows.add(post["author"])
                    interacted = True
                if post and interacted:
                    # Affinity reinforcement is a modeling assumption, not measured psychology.
                    person.trust[post["author"]] = min(1, person.trust.get(post["author"], .3) + .03)
                person.fatigue = min(.8, person.fatigue + .12)
    finally:
        await env.close()
        await client.aclose()
        (root / "events.json").write_text(json.dumps(events, ensure_ascii=False, indent=2), encoding="utf-8")

    with sqlite3.connect(f"file:{database.as_posix()}?mode=ro", uri=True) as db:
        if db.execute("PRAGMA integrity_check").fetchall() != [("ok",)]:
            raise RuntimeError("SQLite integrity failed")
        physical = {table: db.execute(f'PRAGMA foreign_key_check("{table}")').fetchall()
                    for table in ("user", "post", "like", "follow", "comment")}
        if any(errors and not (table == "like" and all(e[2] == "tweet" for e in errors)) for table, errors in physical.items()):
            raise RuntimeError("Unexpected upstream physical FK error")
        semantic = {}
        for table, columns in {"post": [("user_id", "user", "user_id")],
            "like": [("user_id", "user", "user_id"), ("post_id", "post", "post_id")],
            "comment": [("user_id", "user", "user_id"), ("post_id", "post", "post_id")],
            "follow": [("follower_id", "user", "user_id"), ("followee_id", "user", "user_id")]}.items():
            for column, parent, key in columns:
                semantic[f"{table}.{column}"] = db.execute(f'SELECT count(*) FROM "{table}" c LEFT JOIN "{parent}" p ON c."{column}"=p."{key}" WHERE p."{key}" IS NULL').fetchone()[0]
        if any(semantic.values()):
            raise RuntimeError("Logical orphan detected")
        counts = {table: db.execute(f'SELECT count(*) FROM "{table}"').fetchone()[0] for table in ("user", "post", "like", "follow", "comment")}
        comment_authors = db.execute('SELECT COUNT(DISTINCT user_id) FROM comment').fetchone()[0]
        media = {str(p[0]): {"url": f"/seed-assets/community-{p[0]}.jpg", "is_nsfw": False} for p in db.execute('SELECT post_id FROM post')}
    media_path = root / "media.json"
    media_path.write_text(json.dumps(media), encoding="utf-8")
    sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "oasis_adapter"))
    from adapter import export
    checksum = hashlib.sha256(database.read_bytes()).hexdigest()
    export(database, media_path, "hybrid-" + root.name, root / "export")
    if hashlib.sha256(database.read_bytes()).hexdigest() != checksum:
        raise RuntimeError("Adapter changed source DB")
    report = {"mode": "hybrid ABM policy + native Ollama content + public OASIS actions",
        "seed": SEED, "ticks": TICKS, "roles": dict(Counter(p.role for p in people)),
        "parameters": {"population": 100, "max_llm_calls": MAX_CALLS, "max_output_tokens": 128,
                       "temperature": .8, "confidence_range": [.15, .45], "learning_range": [.05, .2]},
        "model": MODEL, "model_digest": model_info["digest"], "ollama": version_response.json(),
        "runtime_source": source, "locked_dependencies": locked, "source_sha256": checksum,
        "physical_fk_checks": physical, "semantic_fk_orphans": semantic, "counts": counts,
        "llm_calls": len(calls), "prompt_tokens": sum(x["response"].get("prompt_eval_count", 0) for x in calls),
        "completion_tokens": sum(x["response"].get("eval_count", 0) for x in calls),
        "rejected_content": rejected, "comment_authors": comment_authors,
        "exposures": sum(exposures.values()), "opinion_update_count": sum(x["delta"] != 0 for x in updates),
        "mutation_gini_including_bootstrap": gini([mutations[p.id] for p in people]),
        "initial_population": initial, "final_population": [p.record() for p in people],
        "opinion_updates": updates,
        "quality_passed": counts["post"] >= 3 and counts["comment"] >= 3 and comment_authors >= 2,
        "limitations": ["Uncalibrated synthetic psychology; not human prediction",
                        "Seed fixes policy RNG, not timestamps or all LLM numerical behavior",
                        "Opening graph and topics are explicit bootstrap",
                        "Custom ABM feed, not evaluation of OASIS or Favi recommendation ranking",
                        "Media URLs are safe metadata only; no image bytes",
                        "Character similarity gate does not establish semantic diversity"]}
    (root / "community-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    from review_community import write_review
    write_review(root)
    print(json.dumps({key: report[key] for key in ("counts", "llm_calls", "prompt_tokens", "completion_tokens", "comment_authors", "quality_passed")}))
    if not report["quality_passed"]:
        raise RuntimeError("Community quality gate failed; inspect raw evidence")


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="backslashreplace")
    root, upstream = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve()
    root.mkdir(parents=True, exist_ok=False)
    asyncio.run(asyncio.wait_for(run(root, upstream, sys.argv[3].rstrip("/")), 900))

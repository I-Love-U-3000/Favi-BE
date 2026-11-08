"""Generate an audited temporal benchmark workload through the real OASIS engine."""
import argparse
import asyncio
from collections import Counter
from contextlib import closing
from datetime import datetime, timezone
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import random
import sqlite3
import sys
import time

from community_policy import population, initial_edges, observe
from dataset_policy import validate_config, fingerprint, canonical, sample_feed, choose_action, content, audit_events
from source_provenance import verify_runtime_source, verify_dependency_lock
from native_projection import audit_database
from dataset_channel import DatasetChannel

PINNED = "ba892e0667eb84b312e4c0e5630f275154bbb98f"


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


async def generate(config, output, upstream, adaptive=False, arrival_policy="uniform", scenario="main"):
    started = time.monotonic()
    output, upstream = output.resolve(), upstream.resolve()
    output.mkdir(parents=True, exist_ok=False)
    write_json(output / "config.resolved.json", config)
    spec = importlib.util.find_spec("oasis")
    if spec is None:
        raise RuntimeError("OASIS environment is missing; follow DATA_GENERATION_FLOW.md setup")
    source = verify_runtime_source(upstream, Path(spec.origin).parent, PINNED)
    locked = verify_dependency_lock(Path(__file__).with_name("requirements-oasis.lock.txt"))
    from dataset_fast import FastFeed, initial_edges_fast, install_indexes
    harness = output / "harness"
    harness.mkdir()
    for name in ("generate_dataset.py", "dataset_policy.py", "dataset_channel.py", "dataset_fast.py", "community_policy.py", "native_projection.py", "source_provenance.py", "requirements-oasis.lock.txt"):
        (harness / name).write_bytes(Path(__file__).with_name(name).read_bytes())
    harness_hashes = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in harness.iterdir()}
    os.chdir(output)
    (output / "log").mkdir()
    import oasis
    from oasis import ActionType
    from oasis.social_agent.agent_action import SocialAction
    from oasis.clock.clock import Clock
    from oasis.social_platform.platform import Platform
    sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "oasis_adapter"))
    from adapter import export

    people = population(config["seed"], config["users"], len(config["topics"]))
    onboarding = None
    if adaptive:
        from adaptive_onboarding import TOPICS, plan, preferences
        if tuple(config["topics"]) != TOPICS:
            raise ValueError("Adaptive generation requires production topic catalog in order")
        onboarding = plan(config["users"], config["seed"], config["ticks"], arrival_policy)
        if arrival_policy == "lifecycle-v2":
            for mode in ('informative', 'noisy', 'independent'):
                write_json(output / f"onboarding.{mode}.json", preferences(onboarding, people, mode))
            onboarding = preferences(onboarding, people, 'informative')
        if scenario == "sparse-topic":
            onboarding = [{**r, 'topics': ['music'] if r['topics'] else []} for r in onboarding]
        write_json(output / "adaptive-policy.json", {"arrival_policy": arrival_policy, "scenario": scenario, "declared_preferences": "informative-v1" if arrival_policy == "lifecycle-v2" else "random-independent-v1", "native_accounts": "bootstrap", "activity_arrival": "logical"})
        write_json(output / "onboarding.json", onboarding)
        (harness / "adaptive_onboarding.py").write_bytes(Path(__file__).with_name("adaptive_onboarding.py").read_bytes())
        harness_hashes["adaptive_onboarding.py"] = hashlib.sha256((harness / "adaptive_onboarding.py").read_bytes()).hexdigest()
    write_json(output / "population.initial.json", [p.record() for p in people])
    rng = random.Random(config["seed"] + 2)
    actions = {k: v for k, v in (("post", ActionType.CREATE_POST), ("comment", ActionType.CREATE_COMMENT), ("like", ActionType.LIKE_POST), ("follow", ActionType.FOLLOW))}
    database = output / "oasis.db"
    os.environ["OASIS_DB_PATH"] = str(database)
    platform = Platform(db_path=str(database), channel=DatasetChannel(), recsys_type="reddit", sandbox_clock=Clock(k=1),
                        start_time=datetime.now(timezone.utc).replace(tzinfo=None))
    engine = install_indexes(platform.db)
    write_json(output / "engine.json", engine)
    agents = [SocialAction(p.id, platform.channel) for p in people]
    platform_task = asyncio.create_task(platform.running())
    events, posts = [], []
    event_file = (output / "events.jsonl").open("w", encoding="utf-8")
    def emit(tick, user, kind, **fields):
        event = {"seq": len(events), "tick": tick, "user_id": user, "bootstrap": tick == -1, "kind": kind, **fields}
        events.append(event)
        event_file.write(canonical(event).decode("utf-8") + "\n")
        return event["seq"]
    def rows(sql):
        with closing(sqlite3.connect(database.as_uri() + "?mode=ro", uri=True)) as db:
            return db.execute(sql).fetchall()
    async def execute(person, kind, arguments):
        # Public SocialAction API performs the same channel request without rebuilding
        # every LLM tool schema or retaining unused ChatAgent memory per mutation.
        result = await getattr(agents[person.id], actions[kind].value)(**arguments)
        if not isinstance(result, dict) or not result.get("success"):
            raise RuntimeError(f"OASIS rejected {kind}: {result}")
        return result
    async def publish(person, topic, tick):
        if scenario == "sparse-topic" and topic == 9 and sum(p['topic'] == 9 for p in posts) >= 9:
            topic = 0
        text = content("post", config["topics"][topic], person.id, len(posts) + 1)
        result = await execute(person, "post", {"content": text})
        post_id = result["post_id"]
        emit(tick, person.id, "post", post_id=post_id, topic=topic, content=text)
        posts.append({"id": post_id, "author": person.id, "topic": topic, "opinion": person.opinion[topic]})
    try:
        for p in people:
            result = await agents[p.id].sign_up(user_name=f"benchmark_{p.id}", name=f"Member {p.id:05d}", bio=f"Synthetic {p.role}")
            if not result.get("success"):
                raise ValueError(f"Native signup failed: {result}")
        if rows("SELECT user_id,agent_id FROM user ORDER BY user_id") != [(p.id, p.id) for p in people]:
            raise RuntimeError("Pinned OASIS user/agent mapping changed")
        for a, b in initial_edges_fast(people, config["seed"]):
            if onboarding and (onboarding[a]["arrival_tick"] != -1 or onboarding[b]["arrival_tick"] != -1):
                continue
            await execute(people[a], "follow", {"followee_id": b})
            emit(-1, a, "follow", target_user_id=b)
            people[a].follows.add(b)
        creators = [p for p in people if p.role == "creator" and (not onboarding or onboarding[p.id]["arrival_tick"] == -1)]
        if not creators:
            raise ValueError("No bootstrap creators")
        for index in range(config["initial_posts"]):
            await publish(creators[index % len(creators)], index % len(config["topics"]), -1)
        feed_sampler = FastFeed(posts)
        for tick in range(config["ticks"]):
            order = people.copy()
            rng.shuffle(order)
            for person in order:
                if onboarding and onboarding[person.id]["arrival_tick"] > tick:
                    continue
                person.fatigue *= .75
                if rng.random() >= config["activity"][person.role] * (1 - person.fatigue):
                    continue
                feed = feed_sampler.sample(person, config, rng)
                exposure = emit(tick, person.id, "exposure", post_ids=[p["id"] for p in feed]) if feed else None
                for post in feed:
                    person.seen.add(post["id"])
                    observe(person, post["topic"], post["opinion"])
                kind, post = choose_action(person, feed, config, rng, len(posts))
                if kind == "post":
                    topic = rng.choices(range(len(config["topics"])), weights=person.interests)[0]
                    await publish(person, topic, tick)
                elif kind == "like":
                    await execute(person, kind, {"post_id": post["id"]})
                    emit(tick, person.id, kind, post_id=post["id"], exposure_seq=exposure)
                    person.liked.add(post["id"])
                elif kind == "comment":
                    text = content(kind, config["topics"][post["topic"]], person.id, len(events))
                    await execute(person, kind, {"post_id": post["id"], "content": text})
                    emit(tick, person.id, kind, post_id=post["id"], exposure_seq=exposure, content=text)
                    person.commented.add(post["id"])
                elif kind == "follow":
                    await execute(person, kind, {"followee_id": post["author"]})
                    emit(tick, person.id, kind, target_user_id=post["author"])
                    person.follows.add(post["author"])
                person.fatigue = min(.8, person.fatigue + .12)
            event_file.flush()
            write_json(output / "progress.json", {"completed_ticks": tick + 1, "events": len(events), "posts": len(posts)})
    finally:
        event_file.close()
        await platform.channel.write_to_receive_queue((None, None, ActionType.EXIT))
        await platform_task

    audit = audit_events(events, config)
    if onboarding:
        for event in events:
            if event["tick"] < onboarding[event["user_id"]]["arrival_tick"]:
                raise ValueError("Action before logical arrival")
            if event["kind"] == "follow" and event["tick"] < onboarding[event["target_user_id"]]["arrival_tick"]:
                raise ValueError("Follow target before logical arrival")
        write_json(output / "adaptive-validation.json", {"passed": True, "users": len(onboarding), "semantics": "logical activity arrival; native accounts provisioned at bootstrap"})
    archive, source_audit = audit_database(database)
    # Require history and final engine state to agree, not only equal row counts.
    actual_likes = {(r["user_id"], r["post_id"]) for r in archive["like"]}
    actual_follows = {(r["follower_id"], r["followee_id"]) for r in archive["follow"]}
    if actual_likes != {(e["user_id"], e["post_id"]) for e in events if e["kind"] == "like"} or actual_follows != {(e["user_id"], e["target_user_id"]) for e in events if e["kind"] == "follow"}:
        raise ValueError("Temporal history differs from final OASIS graph")
    actual_posts = {(r["post_id"], r["user_id"], r["content"]) for r in archive["post"]}
    if actual_posts != {(e["post_id"], e["user_id"], e["content"]) for e in events if e["kind"] == "post"}:
        raise ValueError("Temporal catalog differs from OASIS")
    if Counter((r["user_id"], r["post_id"], r["content"]) for r in archive["comment"]) != Counter((e["user_id"], e["post_id"], e["content"]) for e in events if e["kind"] == "comment"):
        raise ValueError("Temporal comments differ from OASIS")
    write_json(output / "validation.json", {**audit, "source": source_audit})
    if not audit["quality_passed"]:
        raise ValueError("Dataset quality gate failed; raw evidence retained, no export published")
    media = {str(p["post_id"]): {"url": f"/seed-assets/benchmark-{p['post_id']}.jpg", "is_nsfw": False} for p in archive["post"]}
    write_json(output / "media.json", media)
    source_hash = hashlib.sha256(database.read_bytes()).hexdigest()
    manifest = export(database, output / "media.json", "dataset-" + output.name, output / "export")
    if hashlib.sha256(database.read_bytes()).hexdigest() != source_hash:
        raise ValueError("Export mutated OASIS source")
    report = {"schema_version": 1, "mode": "controlled ABM + synthetic corpus + public OASIS actions", "llm_calls": 0,
              "config_sha256": fingerprint(config), "events_sha256": hashlib.sha256((output / "events.jsonl").read_bytes()).hexdigest(),
              "source_sha256": source_hash, "runtime_source": source, "locked_dependencies": locked, "harness_sha256": harness_hashes,
              "counts": manifest["counts"], "quality": audit, "elapsed_seconds": round(time.monotonic() - started, 3),
              "roles": dict(Counter(p.role for p in people)),
              "limitations": ["Synthetic labels and exposure policy, not calibrated human relevance", "No semantic content benchmark: text is a labeled template corpus", "Likes are positives; unobserved/unliked exposures are not ground-truth negatives", "Use event seq/tick for temporal splits; final snapshot is not a training split", "Initial posts/follows are explicit bootstrap", "Media is metadata only; not an image dataset"]}
    write_json(output / "dataset-report.json", report)
    files = {p.relative_to(output).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in output.rglob("*") if p.is_file() and "log" not in p.relative_to(output).parts}
    write_json(output / "dataset-manifest.json", {"schema_version": 1, "files": files, "events_sha256": report["events_sha256"]})
    print(json.dumps({"output": str(output), "counts": report["counts"], "quality": audit, "elapsed_seconds": report["elapsed_seconds"]}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, default=Path(__file__).with_name("configs") / "benchmark.json")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--upstream", type=Path, default=Path(".tools/oasis-upstream"))
    parser.add_argument("--seed", type=int)
    parser.add_argument("--validate-config", action="store_true")
    parser.add_argument("--timeout-seconds", type=int, default=1800)
    parser.add_argument("--adaptive", action="store_true")
    parser.add_argument("--arrival-policy", choices=("uniform", "cutoff-waves-v1", "lifecycle-v2"), default="uniform")
    parser.add_argument("--scenario", choices=("main", "sparse-topic"), default="main")
    args = parser.parse_args()
    if args.arrival_policy != "uniform" and not args.adaptive:
        parser.error("arrival-policy requires --adaptive")
    if args.scenario != "main" and not args.adaptive:
        parser.error("scenario requires --adaptive")
    config = json.loads(args.config.read_text(encoding="utf-8-sig"))
    if args.seed is not None:
        config["seed"] = args.seed
    validate_config(config)
    if not 60 <= args.timeout_seconds <= 14400:
        parser.error("timeout-seconds must be between60 and14400")
    if args.validate_config:
        print(json.dumps(config, indent=2))
        return
    if args.output is None:
        parser.error("--output is required; existing directories are never overwritten")
    args.output = args.output.resolve()
    if args.output.exists():
        parser.error("Output already exists; choose a new directory")
    try:
        asyncio.run(asyncio.wait_for(generate(config, args.output, args.upstream, args.adaptive, args.arrival_policy, args.scenario), timeout=args.timeout_seconds))
    except Exception as error:
        if args.output.exists() and not (args.output / "dataset-manifest.json").exists():
            failure = args.output / "failure.json"
            if not failure.exists():
                write_json(failure, {"error": f"{type(error).__name__}: {error}"})
        raise


if __name__ == "__main__":
    main()

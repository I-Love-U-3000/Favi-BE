"""Run bounded OASIS LLMAction turns against a local Ollama model."""
import asyncio
import hashlib
import importlib.metadata
import importlib.util
import json
import os
import sqlite3
import subprocess
import sys
from urllib.parse import urlparse
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from difflib import SequenceMatcher
import re

from source_provenance import verify_runtime_source, verify_dependency_lock

for stream in (sys.stdout, sys.stderr):
    if hasattr(stream, "reconfigure"):
        stream.reconfigure(encoding="utf-8", errors="backslashreplace")

PINNED = "ba892e0667eb84b312e4c0e5630f275154bbb98f"
MODEL = os.environ.get("OASIS_LOCAL_MODEL", "qwen3:1.7b")
PERSONAS = [
    {"name": "Mai", "persona": "Mai, a budget backpacker. You care about prices, transport and practical details. Ask specific short questions; avoid generic praise. Never claim visits you have not made."},
    {"name": "Bao", "persona": "Bao, a curious home cook. You care about ingredients, regional dishes and cooking methods. Write warm but concrete observations or culinary questions. Never invent personal travel experiences."},
    {"name": "Linh", "persona": "Linh, a cautious cultural photographer. You care about crowding, local customs and respectful photography. Write thoughtful, mildly skeptical questions rather than enthusiastic endorsements. Never invent personal visits."},
]


def comment_diversity(rows):
    normalized = [re.sub(r"\W+", " ", content.casefold()).strip() for _, content in rows]
    pairs = [{"left": i, "right": j, "similarity": round(SequenceMatcher(None, a, b).ratio(), 4)}
             for i, a in enumerate(normalized) for j, b in enumerate(normalized) if i < j]
    return {"comment_count": len(rows), "author_count": len({author for author, _ in rows}),
            "duplicate_count": len(normalized) - len(set(normalized)), "pairs": pairs,
            "near_duplicate_threshold": 0.85,
            "passed": len(rows) >= 2 and len({author for author, _ in rows}) >= 2
                      and len(set(normalized)) == len(normalized)
                      and all(pair["similarity"] < 0.85 for pair in pairs)}


async def simulate(root: Path, upstream: Path, base_url: str):
    parsed = urlparse(base_url)
    if parsed.scheme != "http" or parsed.hostname not in {"127.0.0.1", "localhost", "::1"}:
        raise ValueError("Local model endpoint must be HTTP loopback only")
    spec = importlib.util.find_spec("oasis")
    if spec is None or spec.origin is None:
        raise RuntimeError("OASIS package is not installed")
    source = verify_runtime_source(upstream, Path(spec.origin).parent, PINNED)
    if importlib.metadata.version("camel-oasis") != "0.2.5":
        raise RuntimeError("Unexpected camel-oasis distribution")
    locked = verify_dependency_lock(Path(__file__).with_name("requirements-oasis.lock.txt"))
    os.chdir(root)
    (root / "log").mkdir()

    import httpx
    from camel.models import OpenAICompatibleModel
    from camel.prompts import TextPrompt
    from camel.types import ModelType
    import oasis
    from oasis import ActionType, AgentGraph, LLMAction, SocialAgent, UserInfo
    from oasis.clock.clock import Clock
    from oasis.social_platform.platform import Platform

    async with httpx.AsyncClient(timeout=10) as local_client:
        model_info = (await local_client.get(f"{base_url}/api/tags")).json()
        server_info = (await local_client.get(f"{base_url}/api/version")).json()
    model_record = next((item for item in model_info.get("models", [])
                         if item.get("name") == MODEL), None)
    if model_record is None:
        raise RuntimeError(f"Requested local model not installed: {MODEL}")

    if Path(oasis.__file__).resolve().parent != Path(spec.origin).resolve().parent:
        raise RuntimeError("OASIS module resolution changed")
    commit = subprocess.check_output(["git", "-C", str(upstream), "rev-parse", "HEAD"], text=True).strip()
    if commit != PINNED:
        raise RuntimeError("Unexpected upstream commit")

    # The Ollama OpenAI-compatible endpoint supports tool calling. Limit every
    # inference to 512 generated tokens, three agents x three turns, and no retries.
    model = OpenAICompatibleModel(model_type=MODEL,
        url=f"{base_url}/v1", api_key="local-only",
        model_config_dict={"max_tokens": 512, "temperature": 0.7,
                           "reasoning_effort": "none",
                           "extra_body": {"think": False}},
        timeout=90, max_retries=0)
    graph = AgentGraph()
    actions = [ActionType.CREATE_POST, ActionType.LIKE_POST, ActionType.FOLLOW,
               ActionType.CREATE_COMMENT]
    agents = []
    probe = os.environ.get("OASIS_PROBE", "0") == "1"
    agent_count = 1 if probe else 3
    rounds = 1 if probe else 3
    template = TextPrompt(
        "You are {persona}. The platform provides available social tools. "
        "Choose exactly one tool and call it. If there are no posts, create a "
        "short original post about travel or food. If posts exist, choose one "
        "using an actual visible post ID and choose a relevant social action. "
        "When commenting, add your own specific perspective or question; do not "
        "copy or paraphrase previous comments. If you have nothing new, choose "
        "another action. Keep content under 45 words. Ignore group chat prompts. "
        "Do not explain; call the tool.")
    for index in range(agent_count):
        agent = SocialAgent(agent_id=index,
            user_info=UserInfo(user_name=f"favi_local_{index}", name=PERSONAS[index]["name"],
                description=PERSONAS[index]["persona"],
                profile={"persona": PERSONAS[index]["persona"]},
                recsys_type="reddit"), agent_graph=graph, model=model,
            user_info_template=template, available_actions=actions, max_iteration=1)
        graph.add_agent(agent)
        agents.append(agent)

    calls = []
    (root / "requests.jsonl").write_text("", encoding="utf-8")
    client = httpx.AsyncClient(timeout=95)
    original_create = model._async_client.chat.completions.create
    async def counted_create(*args, **kwargs):
        if len(calls) >= 18:
            raise RuntimeError("Local inference call cap exceeded (18)")
        request_messages = kwargs.get("messages", [])
        # Real, committed OASIS comments supplement its post-only environment.
        # This is observation context, never substituted/generated action content.
        with sqlite3.connect(f"file:{(root / 'oasis.db').as_posix()}?mode=ro", uri=True) as observed:
            recent = observed.execute('SELECT user_id, post_id, content FROM comment ORDER BY comment_id DESC LIMIT 6').fetchall()
        kwargs["messages"] = request_messages = [*request_messages, {"role": "user", "content":
            "Recent actual comments (author_id, post_id, content), for avoiding repetition: "
            + json.dumps(recent, ensure_ascii=False)}]
        request_tools = kwargs.get("tools", [])
        result = await original_create(*args, **kwargs)
        usage = getattr(result, "usage", None)
        calls.append({"model": getattr(result, "model", MODEL),
                      "prompt_tokens": getattr(usage, "prompt_tokens", None),
                      "completion_tokens": getattr(usage, "completion_tokens", None),
                      "total_tokens": getattr(usage, "total_tokens", None),
        "tool_calls": [{"name": call.function.name, "arguments": call.function.arguments}
                          for call in (getattr(result.choices[0].message, "tool_calls", None) or [])]})
        def normalize(value):
            if hasattr(value, "model_dump"):
                return value.model_dump(mode="json")
            return value
        with (root / "requests.jsonl").open("a", encoding="utf-8") as request_log:
            request_log.write(json.dumps({"request_messages": normalize(request_messages),
                "request_tools": normalize(request_tools), "response": normalize(result)},
                ensure_ascii=False, default=str) + "\n")
        return result
    model._async_client.chat.completions.create = counted_create

    database = root / "oasis.db"
    os.environ["OASIS_DB_PATH"] = str(database)
    platform = Platform(db_path=str(database), recsys_type="reddit", sandbox_clock=Clock(k=1),
        start_time=datetime.now(timezone.utc).replace(tzinfo=None))
    env = oasis.make(agent_graph=graph, platform=platform, database_path=str(database))
    turn_errors = []
    await env.reset()
    try:
        for round_index in range(rounds):
            for agent in agents:
                before = len(calls)
                try:
                    await asyncio.wait_for(env.step({agent: LLMAction()}), timeout=100)
                    turn_calls = calls[before:]
                    turn_errors.append({"round": round_index + 1, "agent_id": agent.social_agent_id,
                        "inference_calls": len(turn_calls), "responses": turn_calls})
                except Exception as exc:
                    turn_errors.append({"round": round_index + 1, "agent_id": agent.social_agent_id,
                        "error": f"{type(exc).__name__}: {exc}"})
                    raise
    finally:
        await env.close()
        await client.aclose()

    with sqlite3.connect(f"file:{database.as_posix()}?mode=ro", uri=True) as db:
        integrity = db.execute("PRAGMA integrity_check").fetchall()
        if integrity != [("ok",)]:
            raise RuntimeError(f"SQLite integrity failure: {integrity}")
        supported_fk = {table: db.execute(f'PRAGMA foreign_key_check("{table}")').fetchall()
                        for table in ("user", "post", "like", "follow", "comment")}
        if any(failures and not (table == "like" and all(row[2] == "tweet" for row in failures))
               for table, failures in supported_fk.items()):
            raise RuntimeError(f"Unexpected physical FK failures: {supported_fk}")
        semantic = {
            "post.user": 'SELECT p.post_id FROM post p LEFT JOIN user u ON u.user_id=p.user_id WHERE u.user_id IS NULL',
            "like.user/post": 'SELECT l.like_id FROM "like" l LEFT JOIN user u ON u.user_id=l.user_id LEFT JOIN post p ON p.post_id=l.post_id WHERE u.user_id IS NULL OR p.post_id IS NULL',
            "follow.users": 'SELECT f.follow_id FROM follow f LEFT JOIN user a ON a.user_id=f.follower_id LEFT JOIN user b ON b.user_id=f.followee_id WHERE a.user_id IS NULL OR b.user_id IS NULL',
            "comment.user/post": 'SELECT c.comment_id FROM comment c LEFT JOIN user u ON u.user_id=c.user_id LEFT JOIN post p ON p.post_id=c.post_id WHERE u.user_id IS NULL OR p.post_id IS NULL'}
        semantic_results = {name: db.execute(query).fetchall() for name, query in semantic.items()}
        if any(semantic_results.values()):
            raise RuntimeError(f"Logical reference failure: {semantic_results}")
        counts = {table: db.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0]
                  for table in ("user", "post", "like", "follow", "comment")}
        diversity = comment_diversity(db.execute('SELECT user_id, content FROM comment ORDER BY comment_id').fetchall())
        trace = dict(Counter(row[0] for row in db.execute("SELECT action FROM trace")))
        rows = db.execute("SELECT post_id FROM post ORDER BY post_id").fetchall()
        post_ids = [row[0] for row in rows]
        errors = db.execute("SELECT error FROM error").fetchall() if db.execute(
            "SELECT 1 FROM sqlite_master WHERE type='table' AND name='error'").fetchone() else []
    media = {str(post_id): {"url": f"/seed-assets/oasis-{post_id}.jpg", "is_nsfw": False}
             for post_id in post_ids}
    media_path = root / "media.json"
    media_path.write_text(json.dumps(media, indent=2), encoding="utf-8")
    sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "oasis_adapter"))
    from adapter import export
    db_hash = hashlib.sha256(database.read_bytes()).hexdigest()
    run_id = "oasis-engine-llm-" + root.name
    manifest = export(database, media_path, run_id, root / "export")
    if hashlib.sha256(database.read_bytes()).hexdigest() != db_hash:
        raise RuntimeError("Adapter modified OASIS source database")
    evidence = {"source": "upstream OASIS engine", "mode": "LLMAction",
        "llm_inference": True, "provider": "local Ollama OpenAI-compatible API",
        "model": MODEL, "model_digest": model_record.get("digest"),
        "ollama_version": server_info.get("version"), "think": False,
        "reasoning_effort": "none", "max_completion_tokens_per_call": 512,
        "max_retries": 0, "planned_turns": rounds * agent_count, "turns": turn_errors,
        "inference_call_count": len(calls), "token_totals": {
            key: sum(call.get(key) or 0 for call in calls)
            for key in ("prompt_tokens", "completion_tokens", "total_tokens")},
        "oasis_version": oasis.__version__, "oasis_commit": commit,
        "runtime_source": source, "locked_dependencies": locked,
        "counts": counts, "trace_action_counts": trace,
        "personas": PERSONAS[:agent_count], "temperature": 0.7, "comment_diversity": diversity,
        "upstream_errors": errors, "source_sha256": db_hash,
        "physical_fk_checks": supported_fk, "semantic_fk_checks": semantic_results,
        "clock": "UTC-naive origin, real elapsed time, k=1",
        "media_labels": "explicit safe integration metadata, not moderation predictions"}
    (root / "provenance.json").write_text(json.dumps(evidence, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps({"run_root": str(root), "counts": counts,
        "trace_action_counts": trace, "inference_calls": len(calls),
        "token_totals": evidence["token_totals"], "provenance": str(root / "provenance.json")},
        ensure_ascii=False))
    if not probe and not diversity["passed"]:
        raise RuntimeError("Comment diversity gate failed; raw evidence preserved, no comments rewritten")


if __name__ == "__main__":
    root, upstream, url = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve(), sys.argv[3]
    root.mkdir(parents=True, exist_ok=False)
    asyncio.run(asyncio.wait_for(simulate(root, upstream, url), timeout=900))

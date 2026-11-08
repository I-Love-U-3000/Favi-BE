"""Explicit read-only projection of the full native world into Favi's subset."""
import copy
from contextlib import closing
from datetime import datetime, timedelta, timezone
import hashlib
import json
import re
from pathlib import Path
import sqlite3
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "oasis_adapter"))
import adapter


def mapped_time(value, origin, tick_seconds):
    if isinstance(value, str) and value.isdecimal():
        return (origin + timedelta(seconds=int(value) * tick_seconds)).isoformat()
    if type(value) is int and value >= 0:
        return (origin + timedelta(seconds=value * tick_seconds)).isoformat()
    adapter.timestamp(value)  # Do not fill missing or malformed values.
    return value


def project(raw, origin, tick_seconds=1):
    """Validate core records before exclusions; never edit the raw source."""
    data = copy.deepcopy(raw)
    for table in adapter.REQUIRED_COLUMNS:
        for row in data[table]:
            row["created_at"] = mapped_time(row["created_at"], origin, tick_seconds)
    users = {r["user_id"]: adapter.timestamp(r["created_at"]) for r in data["user"]}
    posts = {r["post_id"]: adapter.timestamp(r["created_at"]) for r in data["post"]}
    if len(users) != len(data["user"]) or len(posts) != len(data["post"]):
        raise ValueError("Duplicate native identity")
    for row in data["post"]:
        created = adapter.timestamp(row["created_at"])
        if row["user_id"] not in users or created < users[row["user_id"]]:
            raise ValueError("Invalid raw post author/time")
        parent = row["original_post_id"]
        if parent is not None and (parent not in posts or created < posts[parent]):
            raise ValueError("Invalid raw repost parent/time")
    for table in ("like", "comment"):
        for row in data[table]:
            created = adapter.timestamp(row["created_at"])
            if row["user_id"] not in users or row["post_id"] not in posts:
                raise ValueError("Raw engagement has an orphan")
            if created < max(users[row["user_id"]], posts[row["post_id"]]):
                raise ValueError("Raw engagement precedes its endpoints")
    excluded_posts = {r["post_id"] for r in data["post"] if r["original_post_id"] is not None}
    exclusions = {"repost_quote_ids": sorted(excluded_posts)}
    data["post"] = [r for r in data["post"] if r["post_id"] not in excluded_posts]
    for table, key in (("like", "like_id"), ("comment", "comment_id")):
        exclusions[table + "_on_repost_quote_ids"] = [r[key] for r in data[table] if r["post_id"] in excluded_posts]
        data[table] = [r for r in data[table] if r["post_id"] not in excluded_posts]
    return data, exclusions


def audit_database(database):
    """Check every declared logical FK, plus missing group member user FK."""
    with closing(sqlite3.connect(database.resolve().as_uri() + "?mode=ro", uri=True)) as db:
        db.row_factory = sqlite3.Row
        db.execute("BEGIN")
        if db.execute("PRAGMA integrity_check").fetchall()[0][0] != "ok":
            raise ValueError("SQLite integrity check failed")
        tables = [r[0] for r in db.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")]
        if any(not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", table) for table in tables):
            raise ValueError("Unexpected native identifier")
        archive = {name: [dict(r) for r in db.execute('SELECT * FROM "' + name.replace('"', '""') + '"')] for name in tables}
        checks, physical, aliases = {}, {}, []
        for table in tables:
            try:
                physical[table] = [list(r) for r in db.execute(f'PRAGMA foreign_key_check("{table}")')]
            except sqlite3.OperationalError as error:
                if table != "group_messages" or '"group_messages" referencing "user"' not in str(error):
                    raise
                physical[table] = {"known_schema_error": str(error)}
            for fk in db.execute(f'PRAGMA foreign_key_list("{table}")'):
                if fk[1] != 0:
                    raise ValueError("Composite FK requires explicit review")
                parent, child_col, parent_col = fk[2], fk[3], fk[4]
                if parent not in tables:
                    if parent == "tweet" and table in {"like", "dislike", "rec"} and parent_col == "post_id":
                        aliases.append({"child": table, "declared": parent, "logical": "post"})
                        parent = "post"
                    else:
                        raise ValueError(f"Unknown missing FK parent {table}->{parent}")
                if not parent_col:
                    raise ValueError("Unspecified FK parent column requires explicit review")
                sql = f'SELECT count(*) FROM "{table}" c WHERE c."{child_col}" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "{parent}" p WHERE p."{parent_col}"=c."{child_col}")'
                checks[f"{table}.{child_col}->{parent}.{parent_col}"] = db.execute(sql).fetchone()[0]
            errors = physical[table]
            if isinstance(errors, list) and errors and not (table in {"like", "dislike", "rec"} and all(r[2] == "tweet" for r in errors)):
                raise ValueError(f"Unexpected physical FK failures: {table}: {errors}")
        if "group_members" in tables:
            checks["group_members.agent_id->user.agent_id"] = db.execute('SELECT count(*) FROM group_members g WHERE NOT EXISTS (SELECT 1 FROM user u WHERE u.agent_id=g.agent_id)').fetchone()[0]
        if any(checks.values()):
            raise ValueError(f"Native source contains logical orphans: {checks}")
        return archive, {"logical_orphans": checks, "physical_diagnostics": physical, "known_parent_aliases": aliases}


def export_projection(database, output, run_id, origin, tick_seconds=1):
    checksum = hashlib.sha256(database.read_bytes()).hexdigest()
    archive, audit = audit_database(database)
    raw = adapter.read_source(database)
    data, exclusions = project(raw, origin, tick_seconds)
    media = {str(p["post_id"]): {"url": f"/seed-assets/native-{p['post_id']}.jpg", "is_nsfw": False} for p in data["post"]}
    snapshot = adapter.normalize(data, run_id, media)
    if output.exists():
        raise ValueError("Refuse to overwrite a projection")
    with tempfile.TemporaryDirectory(prefix=".native-projection-", dir=output.parent) as stage:
        target = Path(stage) / "export"
        target.mkdir()
        payload = json.dumps(snapshot, ensure_ascii=False, indent=2) + "\n"
        (target / "snapshot.json").write_text(payload, encoding="utf-8")
        adapter.write_sql(snapshot, target / "favi-seed.sql")
        manifest = {"mode": "explicit Favi subset of a preserved full OASIS world", "run_id": run_id,
            "source_sha256": checksum, "clock_mapping": {"origin_utc": origin.isoformat(), "seconds_per_tick": tick_seconds},
            "excluded": exclusions, "unmapped_table_counts": {t: len(rows) for t, rows in archive.items() if t not in adapter.REQUIRED_COLUMNS},
            "counts": {key: len(snapshot[key]) for key in ("users", "posts", "likes", "follows", "comments")},
            "files": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in target.iterdir()}}
        (target / "projection-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
        target.rename(output)
    if hashlib.sha256(database.read_bytes()).hexdigest() != checksum:
        raise ValueError("Projection changed the raw database")
    (output.parent / "oasis-all-tables.json").write_text(json.dumps(archive, ensure_ascii=False, indent=2), encoding="utf-8")
    (output.parent / "source-validation.json").write_text(json.dumps(audit, indent=2), encoding="utf-8")
    return manifest, archive, audit

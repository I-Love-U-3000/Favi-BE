"""Convert a paused OASIS SQLite database into a Favi seed and graph snapshot.

Standard library only. No API calls, database writes, or LLM calls.
"""

import argparse
import csv
import hashlib
import json
import sqlite3
import tempfile
import uuid
from contextlib import closing
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urlsplit


REQUIRED_COLUMNS = {
    "user": {"user_id", "user_name", "name", "bio", "created_at"},
    "post": {"post_id", "user_id", "original_post_id", "content", "created_at"},
    "like": {"like_id", "user_id", "post_id", "created_at"},
    "follow": {"follow_id", "follower_id", "followee_id", "created_at"},
    "comment": {"comment_id", "user_id", "post_id", "content", "created_at"},
}


def timestamp(value):
    if not isinstance(value, str) or not value.strip():
        raise ValueError("Every source row requires a non-empty ISO datetime.")
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        raise ValueError(f"Invalid OASIS timestamp: {value!r}") from exc
    # OASIS SQLite timestamps may be naive; the adapter contract treats them as UTC.
    return parsed.replace(tzinfo=parsed.tzinfo or timezone.utc).astimezone(
        timezone.utc
    ).isoformat()


def source_id(value):
    if type(value) is not int or value < 0:
        raise ValueError(f"Expected a non-negative integer OASIS ID, got {value!r}")
    return value


def text(value):
    if value is None:
        return ""
    if not isinstance(value, str) or "\x00" in value:
        raise ValueError("Text fields must be strings without NUL characters.")
    return value


def read_source(path):
    """A read transaction gives one consistent view, including committed WAL data."""
    with closing(sqlite3.connect(path.resolve().as_uri() + "?mode=ro", uri=True)) as db:
        db.row_factory = sqlite3.Row
        db.execute("BEGIN")
        result = {}
        for table, required in REQUIRED_COLUMNS.items():
            columns = {row[1] for row in db.execute(f'PRAGMA table_info("{table}")')}
            missing = required - columns
            if missing:
                raise ValueError(f"Unsupported OASIS schema: {table} missing {sorted(missing)}")
            result[table] = [dict(row) for row in db.execute(f'SELECT * FROM "{table}"')]
        result["ignored_tables"] = sorted(
            row[0] for row in db.execute("SELECT name FROM sqlite_master WHERE type='table'")
            if row[0] not in REQUIRED_COLUMNS
        )
        return result


def normalize(raw, run_id, media):
    """Mandatory validation gate: validate every supported row before publishing."""
    if not run_id.strip():
        raise ValueError("run-id cannot be empty.")
    namespace = uuid.uuid5(uuid.NAMESPACE_URL, "favi:oasis:" + run_id)

    def mapped(kind, key):
        return str(uuid.uuid5(namespace, f"{kind}:{key}"))

    users, posts, likes, follows, comments = [], [], [], [], []
    user_times, post_times = {}, {}

    for row in sorted(raw["user"], key=lambda r: r["user_id"]):
        key = source_id(row["user_id"])
        if key in user_times:
            raise ValueError(f"Duplicate user: {key}")
        created = timestamp(row["created_at"])
        user_times[key] = created
        users.append({
            "id": mapped("user", key), "source_id": key,
            "username": "oasis_" + uuid.UUID(mapped("user", key)).hex,
            "display_name": text(row["name"]) or text(row["user_name"]) or f"Agent {key}",
            "bio": text(row["bio"]), "created_at": created,
        })
    if not users:
        raise ValueError("OASIS database has no users.")

    def require_user(key, created):
        source_id(key)
        if key not in user_times or created < user_times[key]:
            raise ValueError(f"Missing user or action before user creation: {key}")

    def require_post(key, created):
        source_id(key)
        if key not in post_times or created < post_times[key]:
            raise ValueError(f"Missing post or action before post creation: {key}")

    for row in sorted(raw["post"], key=lambda r: r["post_id"]):
        key = source_id(row["post_id"])
        created = timestamp(row["created_at"])
        require_user(row["user_id"], created)
        if key in post_times:
            raise ValueError(f"Duplicate post: {key}")
        if row["original_post_id"] is not None:
            raise ValueError("Reposts/quotes are unsupported; disable those OASIS actions.")
        image = media.get(str(key))
        if not isinstance(image, dict) or type(image.get("is_nsfw")) is not bool:
            raise ValueError(f"media-map needs post {key} with url and reviewed is_nsfw boolean.")
        url = text(image.get("url"))
        parts = urlsplit(url)
        if not ((parts.scheme in {"http", "https"} and parts.netloc)
                or (url.startswith("/seed-assets/") and not parts.scheme and not parts.netloc)):
            raise ValueError(f"Post {key}: image URL must be HTTP(S) or /seed-assets/.")
        post_times[key] = created
        posts.append({
            "id": mapped("post", key), "source_id": key,
            "user_id": mapped("user", row["user_id"]),
            "content": text(row["content"]), "created_at": created,
            "image_url": url, "is_nsfw": image["is_nsfw"],
            "media_id": mapped("media", key),
        })

    for table, target, id_column in [
        ("like", likes, "like_id"), ("follow", follows, "follow_id"),
        ("comment", comments, "comment_id"),
    ]:
        seen_keys, seen_pairs = set(), set()
        for row in sorted(raw[table], key=lambda r: r[id_column]):
            key = source_id(row[id_column])
            created = timestamp(row["created_at"])
            if key in seen_keys:
                raise ValueError(f"Duplicate {table} ID: {key}")
            seen_keys.add(key)
            if table == "follow":
                a, b = row["follower_id"], row["followee_id"]
                require_user(a, created)
                require_user(b, created)
                if a == b:
                    raise ValueError(f"Self follow: {a}")
                item = {"follower_id": mapped("user", a), "followee_id": mapped("user", b)}
            else:
                a, b = row["user_id"], row["post_id"]
                require_user(a, created)
                require_post(b, created)
                item = {"id": mapped(table, key), "user_id": mapped("user", a),
                        "post_id": mapped("post", b)}
                if table == "comment":
                    content = text(row["content"])
                    if not content.strip():
                        raise ValueError(f"Empty comment: {key}")
                    item["content"] = content
            if table != "comment" and (a, b) in seen_pairs:
                raise ValueError(f"Duplicate {table} pair: {(a, b)}")
            seen_pairs.add((a, b))
            target.append({**item, "created_at": created})

    return {"schema_version": 1, "run_id": run_id, "users": users, "posts": posts,
            "likes": likes, "follows": follows, "comments": comments}


def sql_value(value):
    if value is None:
        return "NULL"
    if type(value) is bool:
        return "TRUE" if value else "FALSE"
    if type(value) is int:
        return str(value)
    return "'" + text(value).replace("'", "''") + "'"


def write_sql(snapshot, path):
    """Insert-only seed for the current Favi PostgreSQL schema; never a runtime port."""
    with path.open("w", encoding="utf-8", newline="\n") as out:
        out.write("-- Generated offline OASIS seed. Run with psql -v ON_ERROR_STOP=1.\n")
        out.write("BEGIN;\nSET LOCAL standard_conforming_strings = on;\n")

        def insert(table, columns, values, conflict):
            names = ", ".join(f'"{name}"' for name in columns)
            keys = ", ".join(f'"{name}"' for name in conflict)
            out.write(f'INSERT INTO "{table}" ({names}) VALUES '
                      f'({", ".join(sql_value(v) for v in values)}) '
                      f'ON CONFLICT ({keys}) DO NOTHING;\n')

        for u in snapshot["users"]:
            insert("Profiles", ["Id", "Username", "DisplayName", "Bio", "Role", "CreatedAt",
                                "UpdatedAt", "Version", "PrivacyLevel", "FollowPrivacyLevel", "IsBanned"],
                   [u["id"], u["username"], u["display_name"], u["bio"], 0, u["created_at"],
                    u["created_at"], 1, 0, 0, False], ["Id"])
        for p in snapshot["posts"]:
            insert("Posts", ["Id", "ProfileId", "Caption", "Privacy", "CreatedAt", "UpdatedAt",
                             "Version", "IsArchived", "IsNSFW"],
                   [p["id"], p["user_id"], p["content"], 0, p["created_at"], p["created_at"],
                    1, False, p["is_nsfw"]], ["Id"])
            insert("PostMedias", ["Id", "PostId", "ProfileId", "Url", "ThumbnailUrl", "Position",
                                  "PublicId", "Width", "Height", "Format", "IsAvatar", "IsPoster"],
                   [p["media_id"], p["id"], p["user_id"], p["image_url"], p["image_url"], 0,
                    "oasis/" + p["id"], 0, 0, "unknown", False, False], ["Id"])
        for f in snapshot["follows"]:
            insert("Follows", ["FollowerId", "FolloweeId", "CreatedAt"],
                   [f["follower_id"], f["followee_id"], f["created_at"]], ["FollowerId", "FolloweeId"])
        for r in snapshot["likes"]:
            insert("Reactions", ["Id", "ProfileId", "PostId", "Type", "CreatedAt"],
                   [r["id"], r["user_id"], r["post_id"], 0, r["created_at"]], ["Id"])
        for c in snapshot["comments"]:
            insert("Comments", ["Id", "ProfileId", "PostId", "Content", "CreatedAt", "Version"],
                   [c["id"], c["user_id"], c["post_id"], c["content"], c["created_at"], 1], ["Id"])
        out.write("COMMIT;\n")


def write_csv(path, fields, rows):
    with path.open("w", encoding="utf-8", newline="") as out:
        writer = csv.DictWriter(out, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)


def export(database, media_map, run_id, output):
    if output.exists():
        raise ValueError("Output already exists; use a new directory. No files overwritten.")
    media = json.loads(media_map.read_text(encoding="utf-8-sig"))
    if not isinstance(media, dict):
        raise ValueError("media-map must be a JSON object keyed by OASIS post_id.")
    raw = read_source(database)
    snapshot = normalize(raw, run_id, media)
    output.parent.mkdir(parents=True, exist_ok=True)
    # Publish the directory only after validation and all writes have succeeded.
    with tempfile.TemporaryDirectory(prefix=".oasis-", dir=output.parent) as stage:
        root = Path(stage) / "snapshot"
        root.mkdir()
        payload = json.dumps(snapshot, ensure_ascii=False, indent=2) + "\n"
        (root / "snapshot.json").write_text(payload, encoding="utf-8")
        write_sql(snapshot, root / "favi-seed.sql")
        write_csv(root / "users.csv", ["user_id", "oasis_user_id"],
                  ({"user_id": u["id"], "oasis_user_id": u["source_id"]} for u in snapshot["users"]))
        write_csv(root / "posts.csv", ["post_id", "oasis_post_id", "author_id", "created_at", "is_eligible"],
                  ({"post_id": p["id"], "oasis_post_id": p["source_id"], "author_id": p["user_id"],
                    "created_at": p["created_at"], "is_eligible": int(not p["is_nsfw"])}
                   for p in snapshot["posts"]))
        safe_posts = {p["id"] for p in snapshot["posts"] if not p["is_nsfw"]}
        edges = [{"user_id": r["user_id"], "post_id": r["post_id"], "event_type": "like",
                  "occurred_at": r["created_at"]} for r in snapshot["likes"] if r["post_id"] in safe_posts]
        write_csv(root / "user_post_edges.csv", ["user_id", "post_id", "event_type", "occurred_at"], edges)
        write_csv(root / "user_user_edges.csv", ["follower_id", "followee_id", "created_at"], snapshot["follows"])
        manifest = {"schema_version": 1, "run_id": run_id,
                    "snapshot_sha256": hashlib.sha256(payload.encode("utf-8")).hexdigest(),
                    "counts": {key: len(snapshot[key]) for key in ("users", "posts", "likes", "follows", "comments")},
                    "graph_like_edges": len(edges),
                    "scope": "current-state snapshot, not an event-history reconstruction",
                    "ignored_tables": raw["ignored_tables"],
                    "files": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(root.iterdir())}}
        (root / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        root.rename(output)
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--database", type=Path, required=True)
    parser.add_argument("--media-map", type=Path, required=True)
    parser.add_argument("--run-id", required=True, help="Immutable simulation identity; do not reuse for changed data.")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        manifest = export(args.database, args.media_map, args.run_id, args.output)
    except (ValueError, OSError, sqlite3.Error) as exc:
        parser.exit(1, f"OASIS adapter: {exc}\n")
    print(json.dumps(manifest["counts"]))
    print(f"Validated snapshot: {args.output.resolve()}")


if __name__ == "__main__":
    main()

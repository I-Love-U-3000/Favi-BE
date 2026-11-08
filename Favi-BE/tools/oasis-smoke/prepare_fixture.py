"""Deterministic OASIS-schema fixture, not output from an actual simulator."""
import hashlib
import csv
import json
import sqlite3
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "oasis_adapter"))
from adapter import export

root = Path(sys.argv[1]).resolve()
root.mkdir(parents=True, exist_ok=False)
database = root / "oasis.db"
with sqlite3.connect(database) as db:
    db.executescript('''
    CREATE TABLE user(user_id INTEGER PRIMARY KEY, agent_id INTEGER, user_name TEXT, name TEXT, bio TEXT, created_at TEXT);
    CREATE TABLE post(post_id INTEGER PRIMARY KEY, user_id INTEGER, original_post_id INTEGER, content TEXT, created_at TEXT);
    CREATE TABLE "like"(like_id INTEGER PRIMARY KEY, user_id INTEGER, post_id INTEGER, created_at TEXT);
    CREATE TABLE follow(follow_id INTEGER PRIMARY KEY, follower_id INTEGER, followee_id INTEGER, created_at TEXT);
    CREATE TABLE comment(comment_id INTEGER PRIMARY KEY, user_id INTEGER, post_id INTEGER, content TEXT, created_at TEXT);
    CREATE TABLE trace(action TEXT);
    ''')
    for key in (2, 9, 21):
        db.execute("INSERT INTO user VALUES(?,?,?,?,?,?)", (key, key + 100, f"agent_{key}", f"Tác giả {key}", "Du lịch và ẩm thực", "2026-01-01T00:00:00Z"))
    for key, author, caption in ((15, 2, "Ảnh 'đẹp'; \\ du lịch"), (18, 9, "NSFW fixture"), (22, 21, "Ẩm thực Việt Nam")):
        db.execute("INSERT INTO post VALUES(?,?,?,?,?)", (key, author, None, caption, "2026-01-02T00:00:00Z"))
    db.executemany('INSERT INTO "like" VALUES(?,?,?,?)', [(1, 9, 15, "2026-01-03T00:00:00Z"), (2, 2, 18, "2026-01-03T00:00:00Z"), (3, 2, 22, "2026-01-03T00:00:00Z")])
    db.execute("INSERT INTO follow VALUES(1,9,2,'2026-01-03T00:00:00Z')")
    db.execute("INSERT INTO comment VALUES(1,9,15,?, '2026-01-03T01:00:00Z')", ("Nice'); DROP TABLE Posts; --",))
media = root / "media.json"
media.write_text(json.dumps({str(key): {"url": f"/seed-assets/oasis-{key}.jpg", "is_nsfw": key == 18} for key in (15, 18, 22)}), encoding="utf-8")
before = hashlib.sha256(database.read_bytes()).hexdigest()
manifest = export(database, media, "oasis-favi-integration-v1", root / "export")
if hashlib.sha256(database.read_bytes()).hexdigest() != before:
    raise ValueError("Adapter changed source SQLite")
for name, checksum in manifest["files"].items():
    if hashlib.sha256((root / "export" / name).read_bytes()).hexdigest() != checksum:
        raise ValueError(f"Export checksum mismatch: {name}")
if manifest["graph_like_edges"] != 2:
    raise ValueError("Unexpected safe graph edge count")
snapshot = json.loads((root / "export" / "snapshot.json").read_text(encoding="utf-8"))
safe_ids = {p["id"] for p in snapshot["posts"] if not p["is_nsfw"]}
with (root / "export" / "user_post_edges.csv").open(encoding="utf-8", newline="") as stream:
    edges = list(csv.DictReader(stream))
if {(edge["user_id"], edge["post_id"]) for edge in edges} != {(like["user_id"], like["post_id"]) for like in snapshot["likes"] if like["post_id"] in safe_ids}:
    raise ValueError("Safe graph edges differ from snapshot likes")
print("PASS fixture export, source unchanged, manifest checksums and safe graph edge count")

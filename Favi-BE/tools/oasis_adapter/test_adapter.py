import csv
import json
import sqlite3
import tempfile
import unittest
from pathlib import Path
from contextlib import closing, contextmanager

from adapter import export


@contextmanager
def connect(path):
    with closing(sqlite3.connect(path)) as db:
        with db:
            yield db


# Column names follow the official OASIS social_platform/schema SQL files.
OASIS_SCHEMA = """
CREATE TABLE user(user_id INTEGER PRIMARY KEY, agent_id INTEGER, user_name TEXT,
                  name TEXT, bio TEXT, created_at DATETIME);
CREATE TABLE post(post_id INTEGER PRIMARY KEY, user_id INTEGER, original_post_id INTEGER,
                  content TEXT, created_at DATETIME);
CREATE TABLE "like"(like_id INTEGER PRIMARY KEY, user_id INTEGER, post_id INTEGER, created_at DATETIME);
CREATE TABLE follow(follow_id INTEGER PRIMARY KEY, follower_id INTEGER, followee_id INTEGER, created_at DATETIME);
CREATE TABLE comment(comment_id INTEGER PRIMARY KEY, user_id INTEGER, post_id INTEGER,
                     content TEXT, created_at DATETIME);
CREATE TABLE trace(user_id INTEGER, created_at DATETIME, action TEXT, info TEXT);
"""

# SQLite stand-in for relational checks; does not replace PostgreSQL integration testing.
FAVI_SCHEMA = """
PRAGMA foreign_keys=ON;
CREATE TABLE Profiles(Id TEXT PRIMARY KEY, Username TEXT UNIQUE, DisplayName TEXT, Bio TEXT,
 Role INTEGER, CreatedAt TEXT, UpdatedAt TEXT, Version INTEGER, PrivacyLevel INTEGER,
 FollowPrivacyLevel INTEGER, IsBanned INTEGER);
CREATE TABLE Posts(Id TEXT PRIMARY KEY, ProfileId TEXT REFERENCES Profiles(Id), Caption TEXT,
 Privacy INTEGER, CreatedAt TEXT, UpdatedAt TEXT, Version INTEGER, IsArchived INTEGER, IsNSFW INTEGER);
CREATE TABLE PostMedias(Id TEXT PRIMARY KEY, PostId TEXT REFERENCES Posts(Id),
 ProfileId TEXT REFERENCES Profiles(Id), Url TEXT, ThumbnailUrl TEXT, Position INTEGER,
 PublicId TEXT, Width INTEGER, Height INTEGER, Format TEXT, IsAvatar INTEGER, IsPoster INTEGER);
CREATE TABLE Follows(FollowerId TEXT REFERENCES Profiles(Id), FolloweeId TEXT REFERENCES Profiles(Id),
 CreatedAt TEXT, PRIMARY KEY(FollowerId, FolloweeId));
CREATE TABLE Reactions(Id TEXT PRIMARY KEY, ProfileId TEXT REFERENCES Profiles(Id),
 PostId TEXT REFERENCES Posts(Id), Type INTEGER, CreatedAt TEXT, UNIQUE(ProfileId, PostId));
CREATE TABLE Comments(Id TEXT PRIMARY KEY, ProfileId TEXT REFERENCES Profiles(Id),
 PostId TEXT REFERENCES Posts(Id), Content TEXT, CreatedAt TEXT, Version INTEGER);
"""


class AdapterTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.database = self.root / "oasis.db"
        self.media = self.root / "media.json"
        self.output = self.root / "export"
        with connect(self.database) as db:
            db.executescript(OASIS_SCHEMA)
            # Deliberately different agent_id/user_id and non-consecutive IDs.
            db.executemany("INSERT INTO user VALUES(?,?,?,?,?,?)", [
                (2, 0, "quy", "Quý", "Du lịch", "2026-01-01 00:00:00"),
                (9, 1, "quang", "Quang", "Food", "2026-01-01T07:00:00+07:00"),
            ])
            db.executemany("INSERT INTO post VALUES(?,?,?,?,?)", [
                (15, 2, None, "Ảnh 'đẹp'; \\ travel", "2026-01-02 00:00:00"),
                (18, 9, None, "Sensitive image", "2026-01-02 00:00:00"),
            ])
            db.executemany('INSERT INTO "like" VALUES(?,?,?,?)', [
                (1, 9, 15, "2026-01-03 00:00:00"),
                (2, 2, 18, "2026-01-03 00:00:00"),
            ])
            db.execute("INSERT INTO follow VALUES(1,9,2,'2026-01-03 00:00:00')")
            db.execute("INSERT INTO comment VALUES(1,9,15,?, '2026-01-03 01:00:00')",
                       ("Nice'); DROP TABLE Posts; --",))
        self.media.write_text(json.dumps({
            "15": {"url": "/seed-assets/travel.jpg", "is_nsfw": False},
            "18": {"url": "https://example.org/sensitive.png", "is_nsfw": True},
        }), encoding="utf-8")

    def run_export(self, run_id="test-run", output=None):
        return export(self.database, self.media, run_id, output or self.output)

    def mutate(self, sql):
        with connect(self.database) as db:
            db.execute(sql)

    def test_exports_current_state_with_matching_ids_and_filters_graph(self):
        manifest = self.run_export()
        snapshot = json.loads((self.output / "snapshot.json").read_text(encoding="utf-8"))
        self.assertEqual([u["source_id"] for u in snapshot["users"]], [2, 9])
        self.assertEqual(snapshot["likes"][0]["user_id"], snapshot["users"][1]["id"])
        self.assertEqual(snapshot["users"][0]["created_at"], snapshot["users"][1]["created_at"])
        with (self.output / "user_post_edges.csv").open(encoding="utf-8", newline="") as stream:
            edges = list(csv.DictReader(stream))
        self.assertEqual(len(edges), 1)
        self.assertEqual(edges[0]["post_id"], snapshot["posts"][0]["id"])
        self.assertEqual(manifest["counts"]["likes"], 2)
        self.assertIn("trace", manifest["ignored_tables"])

    def test_deterministic_export_and_namespace_isolation(self):
        self.run_export()
        second = self.root / "second"
        self.run_export(output=second)
        for file in self.output.iterdir():
            self.assertEqual(file.read_bytes(), (second / file.name).read_bytes())
        third = self.root / "third"
        self.run_export("different-run", third)
        self.assertNotEqual((self.output / "users.csv").read_bytes(), (third / "users.csv").read_bytes())

    def test_seed_replay_preserves_relations_unicode_and_sql_literals(self):
        self.run_export()
        sql = (self.output / "favi-seed.sql").read_text(encoding="utf-8")
        # Only this PostgreSQL session setting differs for this SQLite smoke check.
        sql = sql.replace("SET LOCAL standard_conforming_strings = on;\n", "")
        with connect(":memory:") as db:
            db.executescript(FAVI_SCHEMA)
            db.executescript(sql)
            db.executescript(sql)
            self.assertEqual(db.execute("SELECT COUNT(*) FROM Profiles").fetchone()[0], 2)
            self.assertEqual(db.execute("SELECT COUNT(*) FROM Reactions").fetchone()[0], 2)
            self.assertEqual(db.execute("SELECT Role FROM Profiles LIMIT 1").fetchone()[0], 0)
            self.assertEqual(db.execute("SELECT Content FROM Comments").fetchone()[0],
                             "Nice'); DROP TABLE Posts; --")
            self.assertEqual(db.execute("SELECT Caption FROM Posts WHERE IsNSFW=0").fetchone()[0],
                             "Ảnh 'đẹp'; \\ travel")
            self.assertEqual(db.execute("PRAGMA foreign_key_check").fetchall(), [])

    def test_gate_rejects_orphan_without_publishing(self):
        self.mutate('UPDATE "like" SET user_id=999 WHERE like_id=1')
        with self.assertRaisesRegex(ValueError, "Missing user"):
            self.run_export()
        self.assertFalse(self.output.exists())

    def test_gate_rejects_temporal_violation(self):
        self.mutate('UPDATE "like" SET created_at=\'2025-01-01\' WHERE like_id=1')
        with self.assertRaisesRegex(ValueError, "before user creation"):
            self.run_export()

    def test_gate_rejects_duplicate_edge_and_self_follow(self):
        self.mutate("INSERT INTO follow VALUES(2,9,2,'2026-01-03')")
        with self.assertRaisesRegex(ValueError, "Duplicate follow pair"):
            self.run_export()
        self.mutate("UPDATE follow SET follower_id=2 WHERE follow_id=2")
        with self.assertRaisesRegex(ValueError, "Self follow"):
            self.run_export()

    def test_gate_rejects_repost(self):
        self.mutate("UPDATE post SET original_post_id=15 WHERE post_id=18")
        with self.assertRaisesRegex(ValueError, "Reposts/quotes"):
            self.run_export()

    def test_requires_explicit_media_review(self):
        self.media.write_text('{"15": {"url": "/seed-assets/a.jpg"}}', encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "reviewed is_nsfw"):
            self.run_export()

    def test_refuses_missing_schema_or_source_file(self):
        missing = self.root / "missing.db"
        with self.assertRaises(sqlite3.OperationalError):
            export(missing, self.media, "test", self.output)
        self.assertFalse(missing.exists())
        self.mutate('DROP TABLE "like"')
        with self.assertRaisesRegex(ValueError, "Unsupported OASIS schema"):
            self.run_export()

    def test_refuses_overwrite(self):
        self.run_export()
        with self.assertRaisesRegex(ValueError, "Output already exists"):
            self.run_export()

    def test_reads_committed_wal_and_does_not_change_source(self):
        with connect(self.database) as writer:
            writer.execute("PRAGMA journal_mode=WAL")
            writer.execute("UPDATE user SET name='Updated in WAL' WHERE user_id=2")
            writer.commit()
            self.run_export()
            snapshot = json.loads((self.output / "snapshot.json").read_text(encoding="utf-8"))
            self.assertEqual(snapshot["users"][0]["display_name"], "Updated in WAL")
            self.assertEqual(writer.execute("SELECT COUNT(*) FROM user").fetchone()[0], 2)

    def test_rejects_invalid_media_url_and_nul_text(self):
        self.media.write_text(json.dumps({"15": {"url": "file:///private/image.png", "is_nsfw": False}}),
                              encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "image URL"):
            self.run_export()
        self.media.write_text(json.dumps({"15": {"url": "/seed-assets/a.jpg", "is_nsfw": False},
                                          "18": {"url": "/seed-assets/b.jpg", "is_nsfw": True}}),
                              encoding="utf-8")
        with connect(self.database) as db:
            db.execute("UPDATE post SET content=? WHERE post_id=15", ("bad\x00text",))
        with self.assertRaisesRegex(ValueError, "NUL"):
            self.run_export()


if __name__ == "__main__":
    unittest.main()

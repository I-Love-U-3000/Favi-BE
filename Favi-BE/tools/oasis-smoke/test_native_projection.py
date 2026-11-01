import copy
from datetime import datetime, timezone
import unittest
from native_projection import project, mapped_time


class NativeProjectionTests(unittest.TestCase):
    def fixture(self):
        return {"user": [{"user_id": 0, "created_at": 0}],
            "post": [{"post_id": 1, "user_id": 0, "original_post_id": None, "created_at": 1},
                     {"post_id": 2, "user_id": 0, "original_post_id": 1, "created_at": 2}],
            "like": [{"like_id": 1, "user_id": 0, "post_id": 2, "created_at": 3}],
            "comment": [{"comment_id": 1, "user_id": 0, "post_id": 1, "created_at": 3, "content": "don't rewrite this"}],
            "follow": [], "ignored_tables": ["group_messages"]}

    def test_projection_preserves_source_and_reports_exclusions(self):
        raw = self.fixture()
        before = copy.deepcopy(raw)
        data, excluded = project(raw, datetime(2026, 11, 1, tzinfo=timezone.utc))
        self.assertEqual(raw, before)
        self.assertEqual(len(data["post"]), 1)
        self.assertEqual(data["like"], [])
        self.assertEqual(excluded["repost_quote_ids"], [2])
        self.assertEqual(excluded["like_on_repost_quote_ids"], [1])
        self.assertEqual(data["comment"][0]["content"], "don't rewrite this")
        self.assertEqual(data["post"][0]["created_at"], "2026-11-01T00:00:01+00:00")

    def test_orphans_are_not_silently_excluded(self):
        raw = self.fixture()
        raw["like"][0]["post_id"] = 999
        with self.assertRaises(ValueError):
            project(raw, datetime.now(timezone.utc))

    def test_bad_repost_parent_rejected_before_filtering(self):
        raw = self.fixture()
        raw["post"][1]["original_post_id"] = 999
        with self.assertRaises(ValueError):
            project(raw, datetime.now(timezone.utc))

    def test_temporal_error_rejected_before_projection(self):
        raw = self.fixture()
        raw["like"][0]["created_at"] = 0
        with self.assertRaises(ValueError):
            project(raw, datetime.now(timezone.utc))

    def test_no_timestamps_are_fabricated_for_missing_values(self):
        with self.assertRaises((ValueError, TypeError)):
            mapped_time(None, datetime.now(timezone.utc), 60)

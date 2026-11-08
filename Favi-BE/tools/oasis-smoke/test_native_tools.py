import copy
import asyncio
from contextlib import closing
from pathlib import Path
import sqlite3
import tempfile
import unittest
from native_tools import compact_schema, reflection_tool, guarded_callable


class NativeToolsTests(unittest.TestCase):
    def test_compaction_preserves_the_call_contract(self):
        schema = {"type": "function", "function": {"name": "act", "description": "Act now.\nLong return examples.",
            "parameters": {"type": "object", "properties": {"id": {"type": "integer", "description": "A real\n ID"}},
                           "required": ["id"], "additionalProperties": False}, "strict": True}}
        original = copy.deepcopy(schema)
        shorter = compact_schema(schema)
        self.assertEqual(schema, original)
        self.assertEqual(shorter["function"]["name"], "act")
        self.assertEqual(shorter["function"]["parameters"]["required"], ["id"])
        self.assertEqual(shorter["function"]["parameters"]["properties"]["id"]["type"], "integer")
        self.assertTrue(shorter["function"]["strict"])
        self.assertLess(len(str(shorter)), len(str(original)))

    def test_private_reflection_cannot_change_another_actor(self):
        alice, bob, audit = {}, {"emotion": "angry"}, []
        tool = reflection_tool(alice, audit, 0)
        result = tool("a gardener", "calm", "learn", "a quiet morning")
        self.assertTrue(result["success"])
        self.assertEqual(bob, {"emotion": "angry"})
        self.assertEqual(audit[0]["actor"], 0)
        before = dict(alice)
        self.assertFalse(tool("", "", "", "")["success"])
        self.assertEqual(alice, before)

    def test_fk_guard_does_not_delegate_invalid_or_self_follow(self):
        with tempfile.TemporaryDirectory() as directory:
            database = Path(directory) / "fixture.db"
            with closing(sqlite3.connect(database)) as db:
                db.execute("CREATE TABLE user(user_id INTEGER PRIMARY KEY)")
                db.executemany("INSERT INTO user VALUES(?)", [(0,), (1,)])
                db.commit()
            delegated, attempts = [], []
            async def follow(followee_id: int):
                delegated.append(followee_id)
                return {"success": True}
            guarded = guarded_callable(follow, 0, database, attempts)
            self.assertFalse(asyncio.run(guarded(99))["success"])
            self.assertFalse(asyncio.run(guarded(0))["success"])
            self.assertTrue(asyncio.run(guarded(1))["success"])
            self.assertEqual(delegated, [1])
            self.assertEqual(len(attempts), 3)

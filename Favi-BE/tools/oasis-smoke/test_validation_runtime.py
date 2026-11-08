"""Exercise the real fixture entrypoint under interpreter optimization."""
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


class ValidationRuntimeTests(unittest.TestCase):
    def test_fixture_faults_cannot_print_pass_under_optimization(self):
        tools = Path(__file__).resolve().parent
        script = r'''
import hashlib, runpy, sys
from pathlib import Path
tools, root, fault = map(Path, sys.argv[1:])
fault = str(fault)
sys.path.insert(0, str(tools.parent / 'oasis_adapter'))
import adapter
original = adapter.export
def injected(database, *args):
    manifest = original(database, *args)
    target = root / 'export'
    if fault == 'source':
        with database.open('ab') as stream: stream.write(b'fault-injection')
    elif fault == 'checksum':
        with (target / 'favi-seed.sql').open('ab') as stream: stream.write(b'changed')
    elif fault == 'count':
        manifest['graph_like_edges'] = 999
    elif fault == 'edges':
        path = target / 'user_post_edges.csv'
        path.write_text('user_id,post_id\nwrong,wrong\n', encoding='utf-8')
        manifest['files']['user_post_edges.csv'] = hashlib.sha256(path.read_bytes()).hexdigest()
    return manifest
adapter.export = injected
sys.argv = ['prepare_fixture.py', str(root)]
runpy.run_path(str(tools / 'prepare_fixture.py'), run_name='__main__')
'''
        messages = dict(source="Adapter changed source SQLite", checksum="Export checksum mismatch",
                        count="Unexpected safe graph edge count", edges="Safe graph edges differ")
        with tempfile.TemporaryDirectory() as temporary:
            for level in ("0", "1", "2"):
                for fault in ("none", *messages):
                    with self.subTest(optimize=level, fault=fault):
                        root = Path(temporary) / (level + "-" + fault)
                        result = subprocess.run([sys.executable, "-B", "-c", script, str(tools), str(root), fault],
                            env={**os.environ, "PYTHONOPTIMIZE": level}, text=True,
                            capture_output=True, timeout=30)
                        if fault == "none":
                            self.assertEqual(result.returncode, 0, result.stderr)
                            self.assertIn("PASS fixture export", result.stdout)
                        else:
                            self.assertNotEqual(result.returncode, 0)
                            self.assertIn(messages[fault], result.stderr)
                            self.assertNotIn("PASS fixture export", result.stdout)


if __name__ == "__main__":
    unittest.main()

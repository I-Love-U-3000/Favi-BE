"""Behavioral regression checks; stdlib only, no archives or model required."""
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from audit_readiness import run_audit


class IsolationTests(unittest.TestCase):
    def test_hostile_environment_cannot_skip_assertions_or_inject_startup(self):
        for optimize in ('1', '2'):
            with self.subTest(optimize=optimize), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                injected = root / 'injected'
                injected.mkdir()
                marker = root / 'injected.txt'
                (injected / 'sitecustomize.py').write_text(
                    f'from pathlib import Path\nPath({str(marker)!r}).touch()\n', encoding='utf-8')
                script = root / 'audit.py'
                script.write_text("print('audit started', flush=True)\nassert False, 'tampered evidence'\n",
                                  encoding='utf-8')
                log = root / 'failure.log'
                with patch.dict(os.environ, PYTHONOPTIMIZE=optimize, PYTHONPATH=str(injected)):
                    with self.assertRaisesRegex(ValueError, 'failed; see retained log'):
                        run_audit(script, root, log)
                self.assertFalse(marker.exists(), 'PYTHONPATH startup code executed')
                self.assertIn('audit started', log.read_text(encoding='utf-8'))
                self.assertIn('AssertionError: tampered evidence', log.read_text(encoding='utf-8'))

    def test_valid_audit_runs_with_isolated_stdlib_and_no_bytecode(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            script = root / 'audit.py'
            script.write_text(
                "import json, sys\nassert __debug__\nassert sys.flags.isolated\n"
                "assert sys.dont_write_bytecode\nprint(json.dumps({'status': 'PASS'}))\n",
                encoding='utf-8')
            log = root / 'success.log'
            with patch.dict(os.environ, PYTHONOPTIMIZE='2'):
                run_audit(script, root, log)
            self.assertIn('"status": "PASS"', log.read_text(encoding='utf-8'))


if __name__ == '__main__':
    unittest.main()

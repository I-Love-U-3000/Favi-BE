import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

from source_provenance import verify_runtime_source


class ProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.upstream = root / "source"
        self.upstream.mkdir()
        package = self.upstream / "oasis"
        package.mkdir()
        (package / "__init__.py").write_bytes(b"VERSION = 'test'\n")
        (package / "schema.sql").write_bytes(b"CREATE TABLE example(id INTEGER);\n")
        self.git("init", "-q")
        self.git("config", "user.name", "Provenance Test")
        self.git("config", "user.email", "test@example.invalid")
        self.git("config", "core.autocrlf", "false")
        self.git("add", ".")
        self.git("commit", "-qm", "Test source")
        self.commit = self.git("rev-parse", "HEAD").strip()
        self.runtime = root / "installed" / "oasis"
        shutil.copytree(package, self.runtime)

    def git(self, *args):
        return subprocess.check_output(["git", "-C", str(self.upstream), *args], stderr=subprocess.PIPE, text=True)

    def verify(self):
        return verify_runtime_source(self.upstream, self.runtime, self.commit)

    def test_installed_code_and_sql_match_git_blobs(self):
        self.assertEqual(self.verify()["verified_files"], 2)

    def test_windows_line_endings_do_not_change_source_identity(self):
        (self.runtime / "__init__.py").write_bytes(b"VERSION = 'test'\r\n")
        self.assertEqual(self.verify()["verified_files"], 2)

    def test_changed_installed_code_is_rejected(self):
        (self.runtime / "__init__.py").write_bytes(b"VERSION = 'other'\n")
        with self.assertRaisesRegex(RuntimeError, "Runtime OASIS differs"):
            self.verify()

    def test_missing_schema_is_rejected(self):
        (self.runtime / "schema.sql").unlink()
        with self.assertRaisesRegex(RuntimeError, "file set differs"):
            self.verify()

    def test_extra_runtime_module_is_rejected(self):
        (self.runtime / "shadow.py").write_text("raise RuntimeError()", encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "file set differs"):
            self.verify()

    def test_dirty_checkout_is_rejected(self):
        (self.upstream / "oasis" / "__init__.py").write_text("changed", encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "checkout is dirty"):
            self.verify()

    def test_wrong_commit_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, "Unexpected OASIS upstream commit"):
            verify_runtime_source(self.upstream, self.runtime, "0" * 40)


if __name__ == "__main__":
    unittest.main()

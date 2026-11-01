import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from check_dataset import verify


class DatasetIntegrityTests(unittest.TestCase):
    def test_incomplete_output_is_not_reusable(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / "dataset-manifest.json").write_text(json.dumps({"schema_version": 1, "files": {}}))
            with self.assertRaisesRegex(ValueError, "Incomplete"):
                verify(root)

    def test_file_tampering_and_path_escape_are_rejected(self):
        required = {"config.resolved.json", "dataset-report.json", "validation.json", "events.jsonl", "oasis.db", "export/snapshot.json", "export/favi-seed.sql", "export/manifest.json"}
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            files = {}
            for name in sorted(required):
                path = root / name
                path.parent.mkdir(exist_ok=True)
                path.write_bytes(b"original")
                files[name] = hashlib.sha256(b"original").hexdigest()
            (root / "config.resolved.json").write_bytes(b"tampered")
            (root / "dataset-manifest.json").write_text(json.dumps({"schema_version": 1, "files": files}))
            with self.assertRaisesRegex(ValueError, "changed"):
                verify(root)
            # Legacy Windows manifests used backslashes; both forms must load.
            (root / "config.resolved.json").write_bytes(b"original")
            windows_files = {name.replace("/", "\\"): digest for name, digest in files.items()}
            (root / "dataset-manifest.json").write_text(json.dumps({"schema_version": 1, "files": windows_files}))
            with self.assertRaises(json.JSONDecodeError):
                verify(root)  # Payload decoding is reached after every hash passed.
            files = {"../outside": "invalid", **files}
            (root / "dataset-manifest.json").write_text(json.dumps({"schema_version": 1, "files": files}))
            with self.assertRaisesRegex(ValueError, "Invalid manifest path"):
                verify(root)

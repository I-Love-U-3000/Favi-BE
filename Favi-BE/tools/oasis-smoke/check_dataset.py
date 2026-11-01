"""Verify an existing dataset before reuse; standard library only, no simulation."""
import argparse
import hashlib
import json
from pathlib import Path

from dataset_policy import validate_config, audit_events, fingerprint


def verify(root):
    root = root.resolve()
    manifest = json.loads((root / "dataset-manifest.json").read_text(encoding="utf-8"))
    required = {"config.resolved.json", "dataset-report.json", "validation.json", "events.jsonl", "oasis.db", "export/snapshot.json", "export/favi-seed.sql", "export/manifest.json"}
    files = {name.replace("\\", "/"): digest for name, digest in manifest.get("files", {}).items()}
    if manifest.get("schema_version") != 1 or len(files) != len(manifest.get("files", {})) or not required.issubset(files):
        raise ValueError("Incomplete or unsupported dataset manifest")
    for name, expected in files.items():
        path = (root / name).resolve()
        if not path.is_relative_to(root) or path == root or not path.is_file():
            raise ValueError(f"Invalid manifest path: {name}")
        digest = hashlib.sha256()
        with path.open("rb") as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(chunk)
        if digest.hexdigest() != expected:
            raise ValueError(f"Dataset file changed: {name}")
    config = validate_config(json.loads((root / "config.resolved.json").read_text(encoding="utf-8")))
    report = json.loads((root / "dataset-report.json").read_text(encoding="utf-8"))
    events = [json.loads(line) for line in (root / "events.jsonl").read_text(encoding="utf-8").splitlines()]
    audit = audit_events(events, config)
    if not audit["quality_passed"] or audit != report["quality"] or fingerprint(config) != report["config_sha256"]:
        raise ValueError("Dataset quality/configuration mismatch")
    if report["events_sha256"] != files["events.jsonl"] or report["source_sha256"] != files["oasis.db"]:
        raise ValueError("Dataset source fingerprint mismatch")
    return {"verified_files": len(manifest["files"]), "events_sha256": report["events_sha256"], "quality": audit}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", type=Path)
    print(json.dumps(verify(parser.parse_args().root)))

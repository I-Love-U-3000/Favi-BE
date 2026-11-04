"""Create and verify a portable local archive of an immutable dataset bundle."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

from check_dataset import verify


def digest(stream):
    result = hashlib.sha256()
    for chunk in iter(lambda: stream.read(1024 * 1024), b""):
        result.update(chunk)
    return result.hexdigest()


def package(dataset, output):
    dataset, output = dataset.resolve(), output.resolve()
    receipt = output.with_suffix(output.suffix + ".json")
    if output.exists() or receipt.exists() or output.is_relative_to(dataset):
        raise ValueError("Choose a new archive path outside the source dataset")
    integrity = verify(dataset)
    manifest_path = dataset / "dataset-manifest.json"
    manifest_bytes = manifest_path.read_bytes()
    manifest = json.loads(manifest_bytes)
    files = {name.replace("\\", "/"): value for name, value in manifest["files"].items()}
    files["dataset-manifest.json"] = hashlib.sha256(manifest_bytes).hexdigest()
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as archive:
        for name in sorted(files):
            archive.write(dataset / name, dataset.name + "/" + name)
    with zipfile.ZipFile(output) as archive:
        if len(archive.infolist()) != len(files):
            raise ValueError("Archive member count mismatch")
        for name, expected in files.items():
            with archive.open(dataset.name + "/" + name) as stream:
                if digest(stream) != expected:
                    raise ValueError("Archive content fingerprint mismatch: " + name)
    with output.open("rb") as stream:
        checksum = digest(stream)
    report = {"schema_version": 1, "archive": output.name, "sha256": checksum,
              "bytes": output.stat().st_size, "members": len(files), "dataset_directory": dataset.name,
              "events_sha256": integrity["events_sha256"], "quality": integrity["quality"],
              "verification": "Every archived member read back and SHA256-compared with source manifest"}
    with receipt.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps(report))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    package(args.dataset, args.output)

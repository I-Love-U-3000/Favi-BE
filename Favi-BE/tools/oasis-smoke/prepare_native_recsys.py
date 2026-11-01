"""Download the public native OASIS recommender weights into task-owned cache."""
import hashlib
import json
import os
from pathlib import Path
import sys

cache = Path(sys.argv[1]).resolve()
cache.mkdir(parents=True, exist_ok=True)
os.environ["HF_HOME"] = str(cache)
os.environ["HF_HUB_DISABLE_IMPLICIT_TOKEN"] = "1"
os.environ["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "1"
from huggingface_hub import HfApi, snapshot_download

repo = "Twitter/twhin-bert-base"
files = HfApi().list_repo_files(repo, token=False)
weights = "*.safetensors" if any(f.endswith(".safetensors") for f in files) else "pytorch_model*.bin"
snapshot = Path(snapshot_download(repo, revision="main", token=False,
    allow_patterns=["*.json", "*.txt", "*.model", weights], max_workers=3))
fingerprints = {}
for path in sorted(snapshot.rglob("*")):
    if path.is_file():
        digest = hashlib.sha256()
        with path.open("rb") as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(chunk)
        fingerprints[str(path.relative_to(snapshot))] = {"sha256": digest.hexdigest(), "bytes": path.stat().st_size}
report = {"repo": repo, "revision": snapshot.name, "snapshot": str(snapshot), "files": fingerprints}
(cache / "prepared.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({"repo": repo, "revision": snapshot.name, "files": len(fingerprints)}))

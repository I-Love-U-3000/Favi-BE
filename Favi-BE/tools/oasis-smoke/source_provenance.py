"""Verify runtime source against Git blobs, before importing the simulator."""
import hashlib
import importlib.metadata
import subprocess
from pathlib import Path


def git(root, *arguments):
    return subprocess.check_output(["git", "-C", str(root), *arguments], stderr=subprocess.PIPE)


def verify_runtime_source(upstream, package_root, expected_commit):
    upstream, package_root = Path(upstream).resolve(), Path(package_root).resolve()
    commit = git(upstream, "rev-parse", "HEAD").decode().strip()
    if commit != expected_commit:
        raise RuntimeError("Unexpected OASIS upstream commit")
    if git(upstream, "status", "--porcelain", "--untracked-files=all").strip():
        raise RuntimeError("OASIS upstream checkout is dirty")
    paths = git(upstream, "ls-tree", "-r", "--name-only", expected_commit, "--", "oasis").decode().splitlines()
    expected = {name.removeprefix("oasis/") for name in paths if Path(name).suffix in {".py", ".sql"}}
    actual = {path.relative_to(package_root).as_posix() for path in package_root.rglob("*")
              if path.is_file() and path.suffix in {".py", ".sql"}}
    if not expected or expected != actual:
        raise RuntimeError("Runtime OASIS package file set differs from pinned source")
    hashes = {}
    for relative in sorted(expected):
        source = git(upstream, "show", f"{expected_commit}:oasis/{relative}").replace(b"\r\n", b"\n")
        runtime = (package_root / relative).read_bytes().replace(b"\r\n", b"\n")
        if source != runtime:
            raise RuntimeError(f"Runtime OASIS differs from pinned source: {relative}")
        hashes[relative] = hashlib.sha256(runtime).hexdigest()
    tree = "\n".join(f"{name}:{value}" for name, value in hashes.items()).encode()
    return {"commit": commit, "package_root": str(package_root), "verified_files": len(hashes),
            "hash_normalization": "CRLF to LF", "package_tree_sha256": hashlib.sha256(tree).hexdigest(),
            "files_sha256": hashes}


def verify_dependency_lock(path):
    versions = {}
    for line in Path(path).read_text(encoding="utf-8-sig").splitlines():
        if not line.strip() or line.startswith("#"):
            continue
        name, expected = line.split("==", 1)
        actual = importlib.metadata.version(name)
        if actual != expected:
            raise RuntimeError(f"Dependency differs from lock: {name}: {actual} != {expected}")
        versions[name] = actual
    if not versions:
        raise RuntimeError("Empty OASIS dependency lock")
    return versions

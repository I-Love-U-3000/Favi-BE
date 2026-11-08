"""Bounded CLI optimization audit; synthetic manifest fixtures, not OASIS data."""
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
TOOLS = ROOT / "tools/oasis-smoke"
sys.path.insert(0, str(TOOLS))
from dataset_policy import audit_events, fingerprint


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False), encoding="utf-8")


def main(output):
    output.mkdir(parents=True, exist_ok=False)
    config = json.loads((TOOLS / "configs/smoke.json").read_text())
    config.update(initial_posts=10, max_posts=10,
                  quality=dict(min_likes=1, min_warm_users=1, warm_min_likes=1))
    events = [dict(seq=i, tick=-1, user_id=1, bootstrap=True, kind="post", post_id=i+1, topic=0)
              for i in range(10)]
    events += [dict(seq=10, tick=0, user_id=0, bootstrap=False, kind="exposure", post_ids=[1]),
               dict(seq=11, tick=0, user_id=0, bootstrap=False, kind="like", post_id=1, exposure_seq=10)]
    receipts = []
    cases = ("valid", "raw_hash", "missing_entry", "path_escape", "invalid_config",
             "invalid_exposure", "quality_mismatch")
    errors = dict(raw_hash="Dataset file changed", missing_entry="Incomplete or unsupported",
                  path_escape="Invalid manifest path", invalid_config="Use multiples of100",
                  invalid_exposure="Interaction has no preceding valid exposure",
                  quality_mismatch="Dataset quality/configuration mismatch")
    for optimize in ("0", "1", "2"):
        for case in cases:
            folder = output / (optimize + "-" + case)
            folder.mkdir()
            current_config, current_events = copy.deepcopy(config), copy.deepcopy(events)
            if case == "invalid_config":
                current_config["users"] = 101
            if case == "invalid_exposure":
                current_events[-1]["exposure_seq"] = 999
            write(folder / "config.resolved.json", current_config)
            (folder / "events.jsonl").write_text("\n".join(map(json.dumps, current_events)) + "\n", encoding="utf-8")
            # Deliberately opaque bytes: this checks the reuse CLI, not SQLite/Favi validity.
            for name in ("oasis.db", "validation.json", "export/snapshot.json",
                         "export/favi-seed.sql", "export/manifest.json"):
                write(folder / name, {"synthetic_cli_fixture": True})
            sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
            quality = audit_events(events, config)
            if case == "quality_mismatch":
                quality["likes"] += 1
            write(folder / "dataset-report.json", dict(quality=quality,
                  config_sha256=fingerprint(current_config), events_sha256=sha(folder / "events.jsonl"),
                  source_sha256=sha(folder / "oasis.db")))
            files = {p.relative_to(folder).as_posix(): sha(p) for p in folder.rglob("*") if p.is_file()}
            if case == "missing_entry":
                del files["oasis.db"]
            if case == "path_escape":
                files["../outside"] = "0" * 64
            write(folder / "dataset-manifest.json", dict(schema_version=1, files=files))
            if case == "raw_hash":
                (folder / "oasis.db").write_bytes(b"changed")
            for command in ("check_dataset", "package_dataset"):
                args = [sys.executable, "-B", str(TOOLS / (command + ".py"))]
                archive = output / (folder.name + ".zip")
                args += [str(folder)] if command == "check_dataset" else ["--dataset", str(folder), "--output", str(archive)]
                run = subprocess.run(args, env={**os.environ, "PYTHONOPTIMIZE": optimize},
                                     text=True, capture_output=True, timeout=30)
                log = output / (folder.name + "-" + command + ".log")
                log.write_text(run.stdout + run.stderr, encoding="utf-8")
                passed = (run.returncode == 0) == (case == "valid")
                if case != "valid":
                    passed = passed and errors[case] in run.stderr
                if command == "package_dataset" and case != "valid":
                    passed = passed and not archive.exists() and not archive.with_suffix(".zip.json").exists()
                receipts.append(dict(optimize=optimize, case=case, command=command,
                                     exit_code=run.returncode, passed=passed, log=log.name))
    report = dict(status="PASS" if all(r["passed"] for r in receipts) else "FAIL",
                  python=sys.version, checks=len(receipts), receipts=receipts,
                  sources={name: hashlib.sha256((TOOLS / name).read_bytes()).hexdigest()
                           for name in ("check_dataset.py", "dataset_policy.py", "package_dataset.py")})
    write(output / "audit.json", report)
    print(json.dumps(dict(status=report["status"], checks=len(receipts))))
    if report["status"] != "PASS":
        raise SystemExit(1)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())

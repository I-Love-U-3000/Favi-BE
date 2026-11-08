# Independent mapping identity audit — 08/11/2026

Result: all six original confirmatory main/sparse datasets, seeds20261111/12/13,
PASS exhaustive CSV/source-ID/GUID/snapshot namespace equality:30000user rows
and12000post rows. No identity defect demonstrated in this scope. Original
datasets, frozen sources, inventories, archives, model artifacts and ranking
results remain unchanged. No refit, holdout evaluation, SQL or HTTP rerun.

`tools/oasis-smoke/audit_mapping_identity.py` uses only the standard library.
For every user/post it independently reconstructs UUIDv5 from the export run-id,
entity kind and source integer ID, compares snapshot IDs, requires exact CSV
mapping equality with no duplicate source IDs and checks manifest counts. It
also checks the adapter's logical UTF-8 snapshot payload digest. Adapter hashes
before `write_text`, so universal newline normalization is required on Windows;
this digest is not a raw-file byte checksum. Dataset manifests provide the
separate raw-file checksum contract. Initial auditor raw-byte hashing falsely
rejected Windows CRLF; corrected before reporting the six-dataset result.

One unittest covers valid identity, CRLF payload, two bijective CSV GUID
permutations, two duplicate CSV controls, corrupted snapshot checksum and a
coordinated snapshot/CSV/hash edit that violates the deterministic namespace.
These controls test the auditor, not a newly demonstrated exporter defect.
Receipt: ignored `.tools/mapping-identity-audit.json`.

Read-only source review: adapter builds CSV and SQL from the same normalized
snapshot (`tools/oasis_adapter/adapter.py`); `tools/oasis-smoke/Program.cs`
compares every imported entity against that snapshot, even for schema2.
This audit does not execute or parse SQL, query a running database, prove
authenticity against coordinated replacement of the run-id and all evidence,
or replace the historical integration gate. Checksum coverage in the exporter
and semantic identity in this independent audit are distinct checks.

Reproduce from solution root with the existing Python environment:

```powershell
./.tools/oasis-env/Scripts/python.exe -I -B -m unittest discover -s tools/oasis-smoke -p test_mapping_identity.py -v
./.tools/oasis-env/Scripts/python.exe -B tools/oasis-smoke/audit_mapping_identity.py .tools/datasets/adaptive-confirmatory-main-20261111 .tools/datasets/adaptive-confirmatory-main-20261112 .tools/datasets/adaptive-confirmatory-main-20261113 .tools/datasets/adaptive-confirmatory-sparse-20261111 .tools/datasets/adaptive-confirmatory-sparse-20261112 .tools/datasets/adaptive-confirmatory-sparse-20261113
```

All negative relevance/resource conclusions remain unchanged. PR15 evidence
42452493717 and backend42452493819 completed SUCCESS; no review threads at
the earlier check. This audit is local; no hosted coverage claimed.

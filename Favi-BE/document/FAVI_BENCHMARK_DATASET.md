# Favi shared benchmark dataset: favi-5k-v1

Additional 2,000-user and 10,000-user profiles, immutable inventories and scale
comparison rules are documented in [FAVI_DATASET_SCALES.md](FAVI_DATASET_SCALES.md).

## Provisioning contract

This named local dataset uses profile `tools/oasis-smoke/configs/favi-5k.json`,
seed20261101:5000users with exact90/9/1 roles,1500opening posts, max2000posts,
500ticks,8topics and50%feed exploration. Quality gate requires100000distinct likes
and4000users with at least5distinct likes. Reader activity.16 is explicit in the
resolved config; contributor.65/creator.9. This is synthetic benchmark data,
not behavior calibrated against real people. Native OASIS owns all writes.

Canonical working bundle: `.tools/datasets/favi-5k-v1` from the solution root.
Portable backup: `.tools/dataset-archives/favi-5k-v1.zip`; adjacent `.zip.json`
contains archive size, SHA256 and verification receipt. Both remain local and
ignored by Git. They are not a remote/cloud backup. The completed dataset has a
dataset-manifest.json; the archive is complete only with its verified receipt.
Do not edit these artifacts to add experiment predictions or benchmark outputs.

```powershell
# Recreate into a NEW directory, never overwrite the canonical stored copy:
./tools/oasis-smoke/run-dataset.ps1 -Config tools/oasis-smoke/configs/favi-5k.json -Output .tools/datasets/favi-5k-new-run -TimeoutSeconds 7200
# Validate a stored dataset without regeneration:
python tools/oasis-smoke/check_dataset.py .tools/datasets/favi-5k-v1
# Package a new verified copy (refuses an existing archive):
python tools/oasis-smoke/package_dataset.py --dataset .tools/datasets/favi-5k-v1 --output .tools/dataset-archives/favi-5k-v1.zip
```

Archive packaging first verifies the raw bundle. It includes all manifest-declared
files plus the dataset manifest, under one dataset directory; runtime text logs
are excluded. Every ZIP member is read back and SHA256-checked before writing the
receipt. After copying/extracting elsewhere, rerun check_dataset.py. Hashes detect
changes; they are not cryptographic signatures proving authorship.

## Reuse by benchmark type

| Benchmark | Input | Required interpretation |
|---|---|---|
| Popularity / EASE / LightGCN |events.jsonl |Use only prior likes; same temporal splits and candidates |
| PPR / graph traversal |events.jsonl plus user/post mappings |User–post likes and directed user–user follows remain distinct edge types |
| Static graph or loader performance |export/user_post_edges.csv, user_user_edges.csv |Final-state workload; not an earlier training graph |
| Favi SQL import / replay / rollback |export/favi-seed.sql, snapshot.json |Isolated database only; preserve snapshot namespace |
| HTTP feed / reactions / comments / follows |Imported SQL and native Favi API |Use separate authenticated test viewer; seeded profiles are not login accounts |
| Temporal replay / exposure analysis |events.jsonl |seq strictly orders ties within tick; unliked exposure is not a verified negative |

IDs in events/SQLite are source integers. export/users.csv and posts.csv map to
the stable UUIDs of this snapshot namespace. Do not regenerate namespace IDs when
loading a stored copy. No caption topic inference or private preferences are
required for collaborative-filtering features. population.initial.json is latent
simulator state, not relevance labels to give a model an unfair advantage.

Fixed benchmark protocol can use global tick cutoffs300/400: train[0,300),
validation[300,400), test[400,500); bootstrap tick=-1 is visible setup. Build each
training graph from that prefix only. The generator's whole-run warm threshold
does not replace measuring eligible train/test users after these splits. Store
all predictions/results in `.tools/benchmarks/<new-run-id>`, outside the dataset.

The isolated integration gate allows300seconds per bulk SQL import/replay command
for this volume. It still rejects failed transactions and validates complete
snapshot replay/rollback and every imported entity; it does not sample away rows.

This bundle supports graph/ranking/integration workloads. Captions are labeled
templates; media paths are placeholders without image bytes. It is not an image
retrieval, moderation-accuracy, human-relevance or general-purpose social corpus.
100000likes does not guarantee LightGCN will beat simpler methods. Several seeds
and public datasets remain useful for stronger research conclusions.

## Completed generation and archive — 01/11/2026

Generation completed500/500ticks in2601.031seconds (43minutes21seconds) and
passed source/event/quality validation. Exact stored counts:5000users,2000posts,
137198likes,28835follows and20248comments. Likes exceed the approximate100000
target naturally; no truncation or forced interactions were applied. There are
477247exposure events and665528total ordered events; all5000users have at least
5distinct likes. Roles:4500readers,450contributors,50creators. The catalog has
1500bootstrap posts and500later posts; follows include10000bootstrap edges.

Machine-readable inventory: `document/datasets/favi-5k-v1.json`. All paths in
that file are relative to the solution root; counts describe this immutable
bundle, not a requested target. SHA256 fields identify event stream, SQLite and
portable archive. Suggested cutoffs are evaluation metadata, not labels/features.

Raw bundle size is142907866bytes. Portable archive is60285459bytes with23members,
each read back and verified. Its adjacent receipt records SHA256
487297a71762a294d976f79988a7ff930d2a370dc9da5879b0cdb7a1596ca517.
Event stream fingerprint is
a0a9c3cf579fced93425d2000469aa9a09b55ea3900ce0d87ffd331d93b6a0cc.
The bundle validator verified22declared files before packaging; source SQLite
remained unchanged through Favi export. The small-profile SocialAction parity
run retained the original event fingerprint; package/extract/verify roundtrip was
also tested on that smaller real bundle.73unit tests passed for tooling.

## Completed integration and reuse checks

Isolated Favi integration run
`.tools/oasis-smoke/fe16ceabb38b4bffa0449c316e14bcbd` completed with passed=true
and288118PostgreSQL/import/HTTP checks: injected FK rollback, full SQL replay,
every imported entity, paged follower/comment APIs and bounded safe feeds. The
test API listened on127.0.0.1:54790; that address was temporary and the test stack
was cleaned up. Build warnings remain existing application warnings; this is
dataset/integration verification, not a production release certification.

Raw bundle validation was rerun and all22file hashes still matched. ZIP SHA256
was independently reread and still matched its receipt. No regeneration was
needed to reuse the stored dataset.

Popularity-only readiness benchmark from this stored bundle is at
`.tools/benchmarks/favi-5k-v1-popularity-readiness`. The300/400cutoffs yield83083
train likes,27284validation likes and26831test likes. There are4959validation
users and4943test users with held-out targets, all warm; no eligible targets or
users were dropped due to future-created items. The fixed catalog is2000posts.
Test Recall@10=0.0042578007, nDCG@10=0.0036436383. These are synthetic baseline
scores for later comparisons, not evidence of human relevance. This particular
split does not measure cold-user/new-item recommendation; use a separate declared
protocol/dataset for those tasks.

Reuse command:

```powershell
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/benchmark_recommenders.py --dataset .tools/datasets/favi-5k-v1 --output .tools/benchmarks/my-new-run --methods popularity
```

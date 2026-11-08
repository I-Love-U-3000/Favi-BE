# Additional Favi datasets at two scales

## Scope and profiles

Generate two new named bundles without changing favi-5k-v1:

| Dataset | Seed | Users | Initial/max posts | Ticks | Minimum likes |
|---|---:|---:|---:|---:|---:|
| favi-2k-v1 |20261104 |2000 |750/1000 |500 |50000 |
| Existing favi-5k-v1 |20261101 |5000 |1500/2000 |500 |137198 observed |
| favi-10k-v1 |20261105 |10000 |3000/4000 |500 |250000 |

Both new profiles use the same 90/9/1 roles, 8 topics, 5 feed slots, 50% exploration and
activity probabilities (.16/.65/.9) as the 5k profile. Require at least 80% of users
with 5 distinct likes, and run all 500 ticks. No fabricated/truncated interactions to
hit thresholds. Both scale and seed vary, so these are different experimental
conditions, not a controlled estimate of either scale or seed alone. Compare
methods within each dataset; do not equate raw Recall across candidate sizes.

Canonical bundles `.tools/datasets/favi-2k-v1` and `favi-10k-v1`; verified ZIPs
under `.tools/dataset-archives`, with adjacent SHA256 receipts. Native SQLite,
event/exposure history, resolved config, source/harness hashes and Favi SQL/CSV/
snapshot are preserved. Local storage only; no production DB or cloud upload.

## Scaled execution, unchanged policy

dataset_fast.py executes the documented scalar sampling rules with NumPy float64.
Bootstrap distances accumulate topic differences in the same left-to-right order
as Python 3.11 sum; scalar float exponentiation preserves affinity weights. Feed
weights are calculated once per activation when first needed, because follows,
seen state and preferences do not change between feed slots. Removing a selected
item preserves order. NumPy cumulative weights/searchsorted reproduce weighted
choices; uniform selection uses the same Random._randbelow through randrange.
Preserve random branch/draw counts and verify RNG state as well as selected IDs.
The scalar implementations remain as regression references.

The generator uses native public SocialAction clients and Platform.running, not
thousands of unused ChatAgent objects. Native sign_up calls run in ascending actor
order, preserving previous queue write order and user_id/agent_id mapping. Every
mutation is sequential/awaited, verified successful and emitted to the same event
contract. Shutdown uses the original EXIT channel message and awaits platform
completion. No LLM, stub inference or native recommendation call is introduced.

Before the first signup, install five declared nonunique SQLite indexes on the
owned source DB: like(post_id,user_id), follow(follower_id,followee_id),
follow(followee_id), comment(post_id), user(agent_id). These accelerate native
lookups without altering data, table definitions, uniqueness or FK constraints.
They are a deliberate schema overlay, not an upstream package patch. engine.json
archives exact CREATE INDEX statements and an EXPLAIN QUERY PLAN assertion that
the native like lookup uses its index. Known upstream FK DDL defects remain
reported; semantic orphan checks and original adapter validation still apply.
Source SHA256 is measured after the complete run and must remain unchanged by
export. Historical 5k data/harness/archives are never rewritten with this overlay.

Tests compare fast bootstrap graphs with scalar graphs, feed choices and full RNG
states across exploration levels and changing catalogs, and prove the index
overlay does not change table SQL or add uniqueness constraints. A real native
smoke run must retain the historical 7df76a183703caaa510b8089c9d627604189cf7b453cc7484fbe9e36099c970e
event hash before provisioning these new datasets.

## Commands

```powershell
./tools/oasis-smoke/run-dataset.ps1 -Config tools/oasis-smoke/configs/favi-2k.json -Output .tools/datasets/favi-2k-v1 -TimeoutSeconds 14400
./tools/oasis-smoke/run-dataset.ps1 -Config tools/oasis-smoke/configs/favi-10k.json -Output .tools/datasets/favi-10k-v1 -TimeoutSeconds 14400
```

Reuse a stored bundle without launching the simulator:

```powershell
python tools/oasis-smoke/check_dataset.py .tools/datasets/favi-2k-v1
python tools/oasis-smoke/check_dataset.py .tools/datasets/favi-10k-v1
python tools/oasis-smoke/benchmark_recommenders.py --dataset .tools/datasets/favi-2k-v1 --output .tools/benchmarks/favi-2k-new-popularity --methods popularity
python tools/oasis-smoke/benchmark_recommenders.py --dataset .tools/datasets/favi-10k-v1 --output .tools/benchmarks/favi-10k-new-popularity --methods popularity
```

Use the pinned `.tools/oasis-env/Scripts/python.exe` interpreter for numerical
benchmarks. Configs and inventories are versionable; generated bundles, archives
and benchmark outputs remain in ignored `.tools` storage. A manifest and verified
archive receipt are required before marking a generated bundle ready. Copy the
ZIP and receipt together when moving data, then extract and rerun the validator.

Never overwrite an existing bundle. Verify with check_dataset.py and package with
package_dataset.py. Use global 300/400 temporal cutoffs for the first benchmark;
measure actual eligible user/like counts after splitting. Each bundle's native
audit, event/state parity, adapter constraints and hashes must pass; package
members must be read back and hash-checked. Full application/API benchmarking is
a separate run and is not implied by generating a larger snapshot. A lightweight
Popularity run verifies consumption of each stored dataset without regeneration.

## Verified 2k bundle

The completed favi-2k-v1 has 2,000 users, 1,000 posts, 53,607 likes, 10,862 follows
and 7,751 comments. All 2,000 users have at least 5 distinct likes. Its 264,335
events include 191,115 exposures; all 500 ticks completed in 404.938 seconds.
This elapsed time includes concurrent large-dataset generation and is not an
isolated performance measurement. Native audit, full event/state parity and
adapter validation passed. The manifest contains 24 files; the verified ZIP has
25 members and 23,197,502 bytes. Inventory: [favi-2k-v1.json](datasets/favi-2k-v1.json).

Popularity readiness uses 32,777 training likes, 10,537 validation likes and
10,293 test likes. There are 1,984 validation users and 1,971 test users, all warm,
with no future-item positives excluded. Test Recall@10 is 0.010909755578033848
and nDCG@10 is 0.008335496811667635. These establish consumption of the stored
corpus, not superiority over the other dataset sizes. Results are in
`.tools/benchmarks/favi-2k-v1-popularity-readiness`.

The optimized native smoke retained the historical event hash. Tooling tests:
80 simulation/benchmark tests and 12 adapter tests passed. The older 5k manifest
was rechecked and its original event hash remains unchanged. New scale bundles
have not undergone the full Favi PostgreSQL/HTTP integration gate; the existing
5k integration result must not be attributed to them.

## Verified 10k bundle

The completed favi-10k-v1 has 10,000 users, 4,000 posts, 277,340 likes, 58,957
follows and 40,891 comments. All 10,000 users have at least 5 distinct likes.
There are 1,334,295 ordered events, including 953,107 exposures. All 500 ticks
completed in 2,149.906 seconds; this overlaps the smaller run and is not an
isolated speed comparison. Native logical references, event/state parity and
adapter constraints passed. Source SQLite was unchanged by export.

The manifest contains 24 files. Packaging revalidated the bundle and read back
all 25 ZIP members; the archive is 127,999,464 bytes with SHA256
`a6d709244ab5740dbbefeb46d338b7ed1370fc599e3bf34de0e7a1034d5f2c79`.
The event stream SHA256 is
`a6affe8405e58f39ca4cc7560d2ee93b81dcd816fe958c9f382415721c15c3a8`.
Machine-readable inventory: [favi-10k-v1.json](datasets/favi-10k-v1.json).

Popularity readiness yields 167,275 training likes, 55,415 validation likes and
54,650 test likes. There are 9,907 validation users and 9,903 test users, all warm;
no users or positive edges were excluded for future-created target items. Test
Recall@10 is 0.0027081084259145995 and nDCG@10 is 0.001853130841888203. The output
is `.tools/benchmarks/favi-10k-v1-popularity-readiness`. Dataset scale and seed
both differ from the other bundles; these scores are not a causal scale trend.

## Stored dataset inventory

| Dataset | Users | Posts | Likes | Follows | Comments | Test users |
|---|---:|---:|---:|---:|---:|---:|
| favi-2k-v1 | 2,000 | 1,000 | 53,607 | 10,862 | 7,751 | 1,971 |
| favi-5k-v1 | 5,000 | 2,000 | 137,198 | 28,835 | 20,248 | 4,943 |
| favi-10k-v1 | 10,000 | 4,000 | 277,340 | 58,957 | 40,891 | 9,903 |

All three have 500 ticks and fixed 300/400 cutoffs. The two new datasets are ready
for offline ranking/graph benchmarks. Only Popularity readiness was run on them
in this provisioning task; EASE/PPR/LightGCN/Formula comparisons on the two new
scales remain separate experiments. Full PostgreSQL/HTTP integration was not run
on these two new bundles. The previous 5k full integration result remains valid
for the original 5k bundle only.

Subsequent full offline comparisons have now completed on all three stored
datasets. See [THREE_SCALE_RECOMMENDER_BENCHMARK.md](THREE_SCALE_RECOMMENDER_BENCHMARK.md)
for Popularity, Formula V1/V2, EASE, PPR and three-seed LightGCN results. This
extends offline evaluation; it does not add PostgreSQL/HTTP integration coverage.

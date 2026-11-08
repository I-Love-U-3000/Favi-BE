# EASE/PPR feed experiment and end-to-end verification

## Scope and isolation

Adaptive B1/R uses a separate schema2 exporter; see
[adaptive serving contract](ADAPTIVE_SERVING_CONTRACT.md). The legacy endpoint
below still accepts only schema1 EASE/PPR and must not be used to claim R serving.

Action 3 adds an authenticated opt-in experimental feed endpoint. It does not
replace `/api/posts/feed` or `/api/posts/feed-with-reposts`, and defaults to
disabled. Serve only Favi synthetic model artifacts; MovieLens models/data are
not deployed. No schema change or cross-module project reference is required.
The host owns the adapter and bridges existing ContentDiscovery query contracts.
DI registrations remain inside the host ContentDiscovery extension.

Offline Python loads the verified native Favi dataset and archived benchmark
selection, fits EASE and PPR on the declared prefix and exports bounded ranked
candidate lists mapped through the original adapter source-ID/GUID mappings.
This is offline precomputation followed by serving saved ranks, not a claim that
C# runs a dense solver on each request. Retain dataset/model selection hashes,
numeric scores and source IDs in the artifact provenance. No fabricated scores.

## Serving contract

`GET /api/discovery/experiments/feed?method=ease|ppr&page=1&pageSize=10` requires
the existing bearer access token. Disabled endpoint returns 404. Invalid methods
or ranges return 400; no viewer ID is accepted from the client. Identity always
comes from JWT. The feature is read-only; existing HTTP idempotency rules for
mutations remain unchanged.

Configuration section `ExperimentalFeed`: Enabled=false by default, ArtifactPath
an absolute local JSON path, ArtifactSha256 the exact expected digest, and
MaxArtifactAgeHours bounded to 1..168 (default 24). When enabled the snapshot is
loaded once per process, at most 16 MiB, 1,000 users and 300 candidates per method
per user. Require schema/version, nonempty GUIDs, finite descending scores,
unique IDs, both methods and bounded timestamps. The expected hash anchors the
configured artifact; it is not an authorship signature. Bad enabled artifacts
fail clearly; disabled mode does not read a file. There is no hot reload or
distributed cache. Replace artifact/config and restart to change snapshot.

Each request rechecks viewer status, current positive interactions and current
post visibility. Use the existing cold-start reader to identify cold users and
hydrate candidates with public post/profile, active-ban, NSFW, archived/deleted
and own-post filters. Exclude current Like/Love and saved items before pagination;
an interaction added after artifact creation must not be recommended again.
Preserve saved order among remaining candidates. No raw external score is mixed
with production heuristic scores. TotalCount describes the bounded eligible pool.

Cold users use the existing topic/catalog cold-start feed. Warm users without a
snapshot row, with a stale snapshot, or with an empty eligible model pool use the
existing feed query as a fallback, then rehydrate/filter the bounded IDs through
the same conservative public eligibility path. Do not silently relax privacy or
NSFW filtering to fill a page. Caller cancellation propagates.

Response includes method/source/version, bounded candidate count and metrics for
the returned page: distinct-author fraction, maximum-author share and mean age
in hours. These are descriptive serving diagnostics, not relevance metrics or a
fairness guarantee. Age uses real UTC against stored post timestamps; synthetic
virtual-clock content can be old. No freshness improvement is claimed merely by
recording age. HTTP latency must be measured through real localhost requests.

## Verification and limits

Use an isolated PostgreSQL/Redis/API stack with a unique Compose project,
loopback ports, a fresh database and test-only JWT signing key. Build/run .NET
through CLI outside the sandbox; no visible desktop services or error dialogs.
The smoke must train/export actual EASE/PPR models, import the native snapshot,
request both methods over HTTP, verify order and GUID mapping, mutate privacy,
NSFW, archive/delete/ban/current likes, check cold/unknown-model fallbacks,
authorization, invalid pagination, repeated paging and actual HTTP latency.
Stop the child API and only this smoke's containers in finally, preserving other
services. Save failures, API logs and check reports before cleanup.

This is an opt-in integration proof on a bounded native fixture, not a large-scale
serving/load test or approval to enable production traffic. Existing 5k/10k
offline ranking scores do not become HTTP or production-quality guarantees.

## Verified execution

The native fixture has 100 users, 31 posts and 67 trained viewer rows. The saved
model prefix is tick 24. Both EASE/PPR numerical arrays and source-ID mappings
were loaded from the verified native control benchmark before rank export.

| Mode | PostgreSQL/import/HTTP checks | Experimental HTTP requests | Passed |
|---|---:|---:|---|
| Default disabled | 1544 | 2 | yes |
| Fresh EASE/PPR, extended measurement | 1767 | 79 | yes |
| Stale artifact | 1553 | 3 | yes |

All three run reports have passed=true and no cleanup error. The fresh gate
covered real ranking order/GUID mapping, repeatable paging, current reactions
and saves, post/profile privacy, NSFW, own/archived/deleted posts, active author
ban, banned viewer, cold/missing-row/empty-safe-pool fallbacks and return to cold
after removing the last positive. The stale fixture changed only creation time
on a copy of the real artifact; its scores and mappings were retained.

Regression passed: 91 Python smoke/benchmark tests, 12 adapter tests, 24 .NET
unit tests and 67 architecture/privacy tests. Default HTTP authorization gave
401 anonymously and 404 for an authenticated viewer with the feature disabled.

Observed initial HTTP diagnostics (only three requests per method): EASE
median 18.344 ms / p95 53.8638 ms; PPR median 16.7841 ms / p95 24.0138 ms.
The first five-item pages each had author diversity 0.2 and maximum author share
1.0, reflecting the small native creator pool. Mean post age was about 5.56
hours. These describe the integration fixture, not large-scale latency or good
author diversity. An optional 20-request-per-method rerun was not executed:
automatic approval review could not run because its usage limit was reached.
The untested optional loop was reverted to the already-tested implementation.

Full result index: [actions23-20261104.json](benchmarks/actions23-20261104.json).
Raw API logs, individual HTTP responses and all check records remain at the
three run paths listed there. Code and results are saved locally, not committed
or pushed. No production feature flag was enabled. The earlier approval limit
was resolved for the completion run; power-action status is recorded separately
after the final data/source backup checks.

Reproduction uses the existing isolated runner:

```powershell
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/build_feed_artifact.py --dataset .tools/datasets/scale-fast-parity-001 --benchmark .tools/benchmarks/input-kind-native-control-001 --output .tools/feed-experiment/new-artifact
./tools/oasis-smoke/verify.ps1 -Python .tools/oasis-env/Scripts/python.exe -ExistingRunDirectory .tools/datasets/scale-fast-parity-001 -ExperimentalArtifact .tools/feed-experiment/new-artifact/artifact.json
```

Omit ExperimentalArtifact for the disabled-default HTTP gate. SkipToolTests is
available for subsequent configuration-only gate variants after the unchanged
Python suites have passed; the run report records when it is used.

## Completed extended HTTP measurement

The approval service recovered and the additional warm-request run completed.
The earlier blocked attempt and three-request figures above remain historical
evidence. This run reused the same model artifact and reran every fresh safety
check before cleanup; no test containers from any of the four runs remain.

| Method | Requests | HTTP median ms | HTTP p95 ms | Mean ms | Min ms | Max ms |
|---|---:|---:|---:|---:|---:|---:|
| ease | 20 | 13.7173 | 15.0005 | 14.1723 | 12.3375 | 21.1142 |
| ppr | 20 | 13.5956 | 14.7671 | 13.7273 | 11.6867 | 18.1448 |

The sample is sequential, warm, pageSize=10 on the bounded 100-user/31-post native fixture. Scores are precomputed offline; HTTP still performs current eligibility/interaction queries and hydration. These figures are not dense inference speed, a concurrent load test, an SLA or evidence of real-user relevance. Page author diversity/max-author share and real UTC age are retained in the JSON diagnostics.

The latest three mode gates total 4864 checks; including the superseded initial fresh run there were 6551 successful checks. Current source and human/machine reports are additionally backed up under `.tools/handovers/actions23-closeout-001.zip`, with every ZIP member read back and SHA256 checked. Generated data/models and raw logs remain at the already-recorded ignored local paths.

A one-off hidden local helper may request Windows sleep only after a verified completion marker and report/archive hashes match. It uses SetSuspendState with hibernate=false and wake events enabled, enables only its process token sleep privilege, attempts once and records a failure without retrying. It creates no persistent task or power-policy change. OS status is written to `.tools/feed-experiment/gates-002/sleep-status.json`; data reports are not rewritten after scheduling. API reference: [Microsoft SetSuspendState](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-setsuspendstate).

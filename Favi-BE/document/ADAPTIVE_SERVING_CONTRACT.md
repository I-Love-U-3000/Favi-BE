# Adaptive serving artifact schema 2

Historical cost audit completed08/11/2026:
`benchmarks/adaptive-cost-audit-20261108/REPORT_VI.md` and `summary.json`.
Six original elapsed totals12641.235283s;33unique cached refits199.635498s.
Original search-only time and training peak process RAM were never recorded;
remain unavailable, not inferred from model-state bytes/export/API memory.
Audit completion does not pass total resource gates; gateway load remains FAIL.
No retraining, retuning or HTTP rerun in this audit.

08/11/2026. Export implementation: `tools/oasis-smoke/build_adaptive_artifact.py`.
This is a separate artifact from legacy schema1 EASE/PPR; the HTTP
loader now validates both schemas and serves B1/R explicitly. Default disabled
remains unchanged. No ranking method, selection, seed or relevance gate changes.

## Export and provenance

Require original ranking manifest readback, frozen protocol/source inventory,
dependency lock, dataset qualification, selection/report digest, prefix and
onboarding hashes. Refit only saved B1/R parameters on events before cutoff400;
LightGCN uses exact selected epochs, fixed first declared optimizer seed20261103.
This seed is a reproducibility choice, not best-of-three test selection. Shared
family/parameters/seed refits are cached. Original top10 predictions must match
for every exported user with an original target record, including cold routes.
No tuning or reading held-out labels to select exported users or parameters.

Schema2 contains schemaVersion, version, createdAtUtc, trainingCutoffTick,
datasetEventsSha256, selectionSha256, sourceMappingSha256, methods and users.
Methods has exactly B1/R; each declares family, threshold, parameters, epoch and
optimizerSeed. Users contain userId and ranks.B1/ranks.R arrays of postId/score.
Scores are finite real model scores in model rank order; no invented ranks or
offline cold scores are mixed into them. Original export CSVs provide one-to-one
nonempty GUIDs; positive-integer experiment GUIDs are never serving mappings.

Select ascending arrived source IDs, at most1000 users. Save at most300 candidates
per user-method and reject encoded JSON over16MiB before writing artifact.json.
Never call this full5k deployment. Empty warm rank lists for users absent from
the refit user representation are intentional, even if they acquire live likes
later. The original model cannot infer that new history without rebuild.
Output directory must be fresh outside both inputs; preserve partial failures.
Read back output manifest. Provenance records actual bytes, sample rule, hashes,
unique refit seconds/model-state bytes and export/replay seconds. These are
export refits, not original training/search cost or peak process RAM.

## HTTP implementation and pending runtime gates

Controller accepts exact method names ease, ppr, B1 and R. Schema1 carries only
ease/ppr ranks; schema2 carries exactly B1/R metadata and ranks. A method absent
from the configured schema has no model row and uses the explicit fallback,
never an alias. JSON names, frozen parameter keys/values, solver epoch0 and
LightGCN exact epoch10..300/checkpoint10/seed20261103 are validated at startup.
ColdStartQueryReader has an experimental threshold overload restricted to1/5;
the original production overload delegates with1. A database UNION deduplicates
Like/Love post IDs and collection post IDs before bounded threshold counting.
Viewer status is checked before counting/retrieval. Experimental hydration still
excludes current positives and unsafe posts before pagination. Current-positive
filtering belongs to the experiment adapter; production retrieval is unchanged.

Bounded adaptive cold-pool correction08/11/2026: threshold5 can remain cold
with1..4 current positives. Its shared eligible query now excludes the same
Like/Love UNION saves before graph hydration and per-topic20/general60 LIMITs.
Previously those positives consumed bounded slots and were only removed by the
final experiment hydration, which could unnecessarily shorten the eligible pool.
Production threshold1 keeps its original query. Final current-positive and
visibility rechecks remain necessary because live state may change after retrieval.
No unbounded backfill, ranker changes or relaxation of visibility is introduced.

Reproduction fixture:64public untagged posts,4distinct positives comprising
Like/Love/save plus a duplicate Like+save. Before correction only56unseen posts
survived in general top60; after correction all60unseen posts are retrieved.
Focused27.NET tests PASS; actual PostgreSQL smoke11migration/pool checks PASS
including UNION exclusion before LIMIT and unchanged threshold1 warm routing.
The smoke fixture uses a transaction and rolls back its catalog isolation and
inserted rows. Raw before/after TRX and logs: `.tools/cold-pool-eligibility*`.
This is new regression evidence for serving retrieval, not a rerun or rewrite of
frozen offline results, exported artifacts or historical HTTP/latency measurements.

Expose B1/R explicitly without treating them as EASE aliases. Validate schema2
and all bounds/metadata before serving. Count distinct currently positive post
IDs across Like/Love and saves, so repeated signals on one post count once.

Artifact JSON decoding enables `RespectRequiredConstructorParameters` locally
on the snapshot serializer. Every nonoptional constructor field in the artifact,
user, ranked-post and method records must be present, including `score`, `epoch`
and nullable `optimizerSeed`. A nullable value permits explicit JSON null; it
does not permit omission. This prevents missing score from silently becoming0
and missing optimizer provenance from silently becoming null. Schema-specific
optional fields (`methods`, `ease`, `ppr`, `ranks`) retain their constructor
defaults; existing schema validation determines which are required/non-null for
each schema. Missing required fields raise JsonException before startup completes;
checksum verification still runs first. No global HTTP JSON options change.
Explicit score0, solver epoch0 and solver optimizerSeed null remain valid.
This operational positive definition is broader than offline likes-only and
must be reported. Threshold comes from the frozen selected method, not query
parameters. Below threshold use current topic/general cold reader; above it use
saved warm ranks. Keep production threshold1 unchanged. A reader extension must
allow topic retrieval below5 without bypassing current viewer eligibility.

Recheck current viewer/author ban, public post/profile, NSFW, archive/delete,
own posts and current positives before pagination. Never relax privacy to fill
a page. New users, missing learned history, stale artifacts and empty eligible
saved pools use conservative existing fallback with explicit source. New items
may appear through fallback/current cold retrieval; frozen warm ranks cannot
include them until rebuild. Current topics affect cold retrieval immediately.
Warm scores remain frozen: live positive count routes and filters, not retrains.

HTTP lifecycle, topic optimistic version/idempotency and PostgreSQL tests now
pass; direct lab latency passes, rate-limited gateway load fails. See
`benchmarks/adaptive-http-20261108/REPORT_VI.md` and `summary.json` for evidence.
Frozen lab gate:10concurrent,100warmup+1000measured,
at least5process restarts, warm p95<=100ms; direct/gateway results separate.
Original training/search/refit/export/load and process peak RAM must be reported
separately. Passing exporter tests does not satisfy these gates, change primary
practical FAIL or prove real-user relevance.

### Schema2 HTTP verification runner

`tools/oasis-smoke/ExperimentalFeedChecks.cs` dispatches schema2 artifacts to
`AdaptiveFeedChecks`, retaining the legacy schema1 checks. The isolated runner
imports the original native GUID snapshot into fresh PostgreSQL/Redis, verifies
transaction rollback and replay, and checks rank GUID membership. Schema2 skips
the legacy exhaustive profile/post HTTP catalog walk; database import validation
remains exhaustive. It is an operational lifecycle fixture, not offline replay:
one original ranked viewer's live positives are reset and added deterministically
in the disposable database to test counts 0 through 5, including a duplicate
Like/save and distinct saves alone. No source dataset, artifact scores, selection,
training inputs or frozen research results are changed.

The runner checks authentication/input bounds, current filtering, original rank
order before pagination, topic version/idempotent replay, fresh user/item/topic
changes, missing/empty model fallbacks and ban. It writes raw HTTP results to
`adaptive-http.json`. Fresh-process observations (five separate processes) record
start-to-health and peak working set at readiness in `adaptive-restarts.json`;
these include host startup/database work and are not artifact-load-only timings
or disk-cache-cold measurements. The final process memory report includes the
HTTP workload; OS peak working set is resident process memory, not managed heap
or full training/search RAM.

Each B1/R direct load uses concurrency10,100warmup+1000measured, records every
latency and validates response source/positive exclusion/NSFW. Nearest-rank p95
is sample950 of1000 sorted ascending. Failed latency gates are retained without
skipping subsequent privacy checks. Functional smoke success alone does not
mean resource gates pass.

`AdaptiveGatewayProbe` uses a uniquely named disposable NGINX single-upstream
container with loopback publication and the production REST rate150/minute and
burst30. It runs the same request counts, reports status counts and requires all
requests successful and safe for the latency gate. A quick429 cannot pass this
gate. Gateway errors and cleanup failures are retained separately. This limited
lab proxy does not verify production multi-replica failover, TLS, SignalR sticky
sessions or all gateway routes; production gateway configuration is unchanged.
Transport results must remain separate. The two methods share the gateway IP
rate bucket, as real requests from the same client do; it is not reset between
methods. No rate-limit bypass or post-hoc tuning is allowed to manufacture PASS.

## Verification

Schema-presence correction08/11/2026: three focused removal fixtures recompute
the checksum after deleting score/epoch/optimizerSeed, so the parser is actually
exercised. Before correction score and optimizerSeed were accepted; epoch already
failed later semantic validation. After correction all three fail at JSON decoding,
while the complete zero/null fixture succeeds. Raw before/after/final TRX and
logs are retained in `.tools/schema-integrity` and `.tools/schema-integrity-*.log`.
The final focused suite now has26cases. This does not change frozen artifacts,
scores, selections, schema versions, production enablement or research conclusions.
The actual250user artifact11867251bytes also loads with B1threshold1/Rthreshold5;
diagnostic load364.4894ms is a single probe, not a new latency benchmark. Probe
log `.tools/schema-integrity-actual-artifact.log`; original artifact bytes unchanged.

Loader/routing milestone:23 .NET tests PASS with filter
`FullyQualifiedName~ExperimentalFeedTests|FullyQualifiedName~ColdStart`.
The DB-reader fixtures use EF InMemory, real cold reader/snapshot/experimental
reader and a controlled regular-feed proxy that deliberately returns IDs whose
visibility later changes; final hydration must remove them. They do not verify
PostgreSQL SQL translation or HTTP middleware. Cases cover schema1 regression,
schema2 families/invalid metadata, current4-to5 routing, Like+save deduplication,
viewer ban, positive removal, pre-pagination exclusion/order and safe fallback.
An initial test compile used Items instead of Data; corrected. The first routing
fixture lacked regular-reader DI dependencies; replaced with the explicit proxy.
Neither failure involved original model/data/results.

Actual exported250user artifact11867251bytes loaded successfully through the .NET
snapshot with B1threshold1/Rthreshold5 and original version. One diagnostic load
took355.0113ms, including checksum/parse/validation; this is neither restart-gate
evidence nor a latency distribution. Raw commands/logs/probe source remain in
`.tools/adaptive-loader-probe`, `.tools/adaptive-loader-probe.log` and
`.tools/adaptive-loader-tests.log`. Existing NU1903 SignalR and EF relational
version-conflict warnings remain; this milestone did not update dependencies.
Subsequent schema2 HTTP milestone verifies isolated PostgreSQL UNION/HTTP,
topic version/idempotency, stale lifecycle,5restarts and10concurrency100+1000.
Direct latency passes; NGINX single-upstream gateway load fails on429 at the
unchanged150/minute rate. Full original training/search cost/peak RAM remains
pending; measured API process working set does not replace that evidence.

Two unittest cases include real EASE/PPR/LightGCN fits, refit cache, exact original
prediction replay, source GUID identity, unknown-history empty rows, prior-like
exclusion, deterministic bounded sampling, wrong optimizer rejection, changed
manifest rejection and overwrite rejection. Data gate/lock/tuner are explicitly
mocked only in the small orchestration fixture; full qualification is required
by the actual CLI.

```powershell
./.tools/oasis-env/Scripts/python.exe -X utf8 -m unittest discover -s tools/oasis-smoke -p test_adaptive_artifact.py -v
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/build_adaptive_artifact.py --dataset .tools/datasets/adaptive-confirmatory-main-20261111 --ranking .tools/benchmarks/adaptive-confirmatory-ranking-20261108/adaptive-confirmatory-main-20261111 --output .tools/feed-experiment/adaptive-export-20261108-main11-250 --max-users 250
```

Actual export250users PASS;1000users rejected by16MiB bound. Use fresh output
paths on reproduction. Evidence: benchmarks/adaptive-serving-export-20261108.

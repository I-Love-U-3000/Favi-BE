# Adaptive serving artifact schema 2

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

Expose B1/R explicitly without treating them as EASE aliases. Validate schema2
and all bounds/metadata before serving. Count distinct currently positive post
IDs across Like/Love and saves, so repeated signals on one post count once.
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

HTTP lifecycle, topic optimistic version/idempotency, fallback/privacy and
resource tests remain pending. Frozen lab gate:10concurrent,100warmup+1000measured,
at least5process restarts, warm p95<=100ms; direct/gateway results separate.
Original training/search/refit/export/load and process peak RAM must be reported
separately. Passing exporter tests does not satisfy these gates, change primary
practical FAIL or prove real-user relevance.

## Verification

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
Actual isolated HTTP, PostgreSQL translation, topic version/idempotency, stale
lifecycle,5restarts,10concurrency100+1000 and full cost/peak RAM remain pending.

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

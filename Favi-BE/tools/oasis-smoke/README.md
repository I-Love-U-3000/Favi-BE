# OASIS → Favi integration smoke

Independent interaction-data validation now has an explicit `--dataset-kind
movielens100k` input in `run_expanded_benchmark.py`; see
[EXTERNAL_RECOMMENDER_BENCHMARK.md](../../document/EXTERNAL_RECOMMENDER_BENCHMARK.md).
`build_feed_artifact.py` exports verified EASE/PPR NPZ ranks through native GUID
mappings. Pass `-ExperimentalArtifact <absolute-artifact-path>` to `verify.ps1`
for the opt-in HTTP experiment; omit it to verify default-disabled behavior.
Serving configuration, limits, checked gates and result locations are in
[EXPERIMENTAL_RECOMMENDER_FEED.md](../../document/EXPERIMENTAL_RECOMMENDER_FEED.md).

## Recommended dataset flow

For recommendation/graph workloads, use `./tools/oasis-smoke/run-dataset.ps1`.
The default versioned JSON profile creates1000users and a300-post opening catalog
through public OASIS actions, without inference or a model service. All exposures
and interactions are recorded in strict sequence for later temporal evaluation.
Use `-Config tools/oasis-smoke/configs/smoke.json -VerifyFavi` for the smaller
end-to-end integration profile; `-Seed` creates an independent replicate.
Configuration, one-time setup, modeling assumptions and artifact contracts:
[DATA_GENERATION_FLOW.md](../../document/DATA_GENERATION_FLOW.md).
The autonomous/native runners below are separate behavior experiments.

The larger reusable Favi dataset profile is `configs/favi-5k.json`; provision with
`-Config tools/oasis-smoke/configs/favi-5k.json -TimeoutSeconds 7200`. Canonical
local storage and reuse instructions are in
[FAVI_BENCHMARK_DATASET.md](../../document/FAVI_BENCHMARK_DATASET.md).
The full three-scale evaluation protocol and result tables are recorded in
[THREE_SCALE_RECOMMENDER_BENCHMARK.md](../../document/THREE_SCALE_RECOMMENDER_BENCHMARK.md).
For fixed-scale seed stability, use `configs/recommenders/multiseed.json` and
`summarize_multiseed_benchmark.py`; see
[MULTISEED_RECOMMENDER_BENCHMARK.md](../../document/MULTISEED_RECOMMENDER_BENCHMARK.md).
Additional 2,000-user and 10,000-user profiles, execution details and validation
contracts are documented in [FAVI_DATASET_SCALES.md](../../document/FAVI_DATASET_SCALES.md).
`package_dataset.py --dataset <stored-bundle> --output <new-archive.zip>` verifies
the bundle, creates a portable ZIP and reads back every member to check hashes;
the adjacent .zip.json receipt marks a successfully verified archive.

For offline ranking evaluation, run `benchmark_recommenders.py --dataset
<existing-dataset> --output <new-benchmark-directory>` with this Python environment.
It verifies dataset integrity, evaluates Popularity and the current warm-feed
formula port on the same temporal candidates/targets and writes per-user results,
latency/allocation measurements and a hash manifest. Protocol configuration lives
under configs/recommenders; dataset-generation profiles stay at configs/*.json.
See [RECOMMENDER_BENCHMARK.md](../../document/RECOMMENDER_BENCHMARK.md) for results
and the distinction between offline scoring and production feed behavior.

`run_formula_benchmark.py --dataset <stored-dataset> --output <new-run>` tunes
12predeclared Formula V2 configurations using validation labels only, persists
the selection, then compares Popularity/V1/V2 on the same temporal test population.
It records author concentration, ranking quality/cost and paired user bootstrap
intervals. See [FORMULA_V2_BENCHMARK.md](../../document/FORMULA_V2_BENCHMARK.md).

`run_expanded_benchmark.py --dataset <stored-dataset> --output <new-run>` adds
validation-selected EASE, exact bipartite PPR and CPU LightGCN with3test training
seeds. Use `--formula-run <completed-formula-run>` to attach verified historical
V1/V2 references, separately labeled for their additional signals. Numeric model
search settings live in configs/recommenders/expanded.json. Existing dependencies
are locked; inference computes scores per query rather than caching all-user scores.
See [EXPANDED_RECOMMENDER_BENCHMARK.md](../../document/EXPANDED_RECOMMENDER_BENCHMARK.md).

## Actual OASIS engine mode

The official source is cloned under ignored `.tools/oasis-upstream`, pinned to
`ba892e0667eb84b312e4c0e5630f275154bbb98f` (OASIS 0.2.5). Install its dependencies
into a separate `.tools/oasis-env` Python 3.11 environment, with CPU PyTorch.

```powershell
./tools/oasis-smoke/verify.ps1 -Simulator -Python .tools/oasis-env/Scripts/python.exe
```

This calls real `oasis.make`, `env.reset`, sequential `env.step(ManualAction)` and
`env.close`. Only OASIS creates the schema and writes source users/posts/interactions.
The runner reads its SQLite and actual trace, exports through the unchanged Favi
adapter, then executes the same PostgreSQL replay/rollback/API checks. Assertions
use snapshot counts/values rather than hardcoded synthetic IDs or captions.
`provenance.json` records source commit, mode, counts, trace and source checksum.
Before importing OASIS, verify the installed package's complete Python/SQL file
set and canonical SHA-256 against the pinned Git blobs (CRLF is normalized to LF).
Reject dirty upstream checkouts, wrong commits, missing/extra/modified package
files and wrong distribution versions. Verify all 130 dependencies against
`requirements-oasis.lock.txt`; provenance records actual module path, file hashes
and locked versions. This lock describes the verified Windows Python 3.11 CPU
environment; it is not a universal Linux lock (it includes pywin32).
Reinstall the runtime with the lock when a mismatch is detected; do not silently
update it:

```powershell
uv pip install --python .tools/oasis-env/Scripts/python.exe --torch-backend cpu --constraint tools/oasis-smoke/requirements-oasis.lock.txt .tools/oasis-upstream
```

Every actual simulation gets an immutable `oasis-engine-manual-<runGUID>` snapshot
namespace: timestamps change between runs, so a fixed run-id must not be reused.

This is a real simulator engine run with scripted actions, **not autonomous LLM
agents**. A never-inferred StubModel satisfies the SocialAgent constructor; there
are no LLMAction requests, API calls or model decisions. Three agents perform
create-post/like/follow/comment; media URLs and restriction labels are explicit
test metadata, not generated images or moderation predictions. The UTC-naive
simulation origin uses the public Platform/Clock API to satisfy the adapter UTC
contract on Windows without changing OS timezone or upstream code.

The manual engine mode above does not use LLMAction. A separate bounded
autonomous run has now been completed with local Ollama `qwen3:1.7b`; see
`document/OASIS_SIMULATOR_VERIFICATION.md` for provenance, token totals, outcomes,
known behavior limits and the PostgreSQL/HTTP integration evidence.

To reproduce from the solution directory, start Ollama bound to loopback and
mount the ignored local model cache. Use no paid API key:

```powershell
docker run -d --name favi-oasis-ollama -p 127.0.0.1:11439:11434 -v "${PWD}/.tools/ollama-models:/root/.ollama" ollama/ollama:latest
docker exec favi-oasis-ollama ollama list
$env:OASIS_LOCAL_MODEL = 'qwen3:1.7b'
./.tools/oasis-env/Scripts/python.exe tools/oasis-smoke/run_autonomous.py .tools/oasis-smoke/llm-replay-<unique-id> .tools/oasis-upstream http://127.0.0.1:11439
./tools/oasis-smoke/verify.ps1 -ExistingRunDirectory .tools/oasis-smoke/llm-replay-<unique-id> -Python .tools/oasis-env/Scripts/python.exe
docker rm -f favi-oasis-ollama
```

The run uses three agents x three sequential LLMAction turns, max 512 completion
tokens/call, zero retries and a hard cap of 18 inference requests. It captures
request/response evidence and model token usage. Endpoint must be HTTP loopback.
The integration command imports the generated adapter export into isolated test
PostgreSQL and runs the normal API gate. Its evidence must be reviewed for each
new run; run IDs and HTTP ports are unique.

The pinned upstream declares group_messages.sender_id → user.agent_id without a
UNIQUE parent key, and like.post_id → a missing tweet table instead of post.
Simulator mode reports physical FK diagnostics unchanged, then checks every logical
reference of post/like/follow/comment against the real user/post tables by anti-join.
Every semantic orphan fails the run; only those exact upstream DDL issues are
reported as known. Source schema is never patched to force a pass.

Run from the solution folder with Docker, .NET SDK 10/runtime 9 and Python 3.10+:

```powershell
./tools/oasis-smoke/verify.ps1
# Or supply the actual Python executable:
./tools/oasis-smoke/verify.ps1 -Python C:/path/to/python.exe
```

Use CLI outside sandbox when localhost/Docker/process access is restricted.
The script runs the adapter's 12 tests, generates a deterministic synthetic SQLite
fixture, exports using the actual adapter, checks source SHA-256 and manifest
checksums, builds the real API/client and starts a dedicated Compose project.

PostgreSQL 15: localhost:15449, Redis 7: localhost:16399. Kestrel binds
127.0.0.1:0 and atomically obtains a free port. The runner reads the listening
address from that child process's stdout and verifies the process is alive before
health/HTTP writes. The actual URL is recorded in report/checks.json; it never
discovers the API by probing a shared fixed port. test-api-isolation.ps1 holds the
old 18191 port during the full gate and asserts it receives no connections.
Only hardcoded smoke credentials and newly generated `oasis_import_<GUID>` and
`oasis_rollback_<GUID>` databases are used. Application database/config is not used.
Migrations are from current main (including cold-start). No simulator/model/API key.

Three profiles, three posts (two safe, one NSFW), three likes, one follow and one
comment are imported sequentially. Verify every imported field is unchanged after
replay. In another database, inject a real FK violation before COMMIT: all imported
rows must roll back while a preexisting sentinel profile remains. No DROP/TRUNCATE.
The original SQL is sent unchanged to PostgreSQL through Npgsql, not SQLite.
Source timestamps with offsets are compared using DateTimeOffset.UtcDateTime,
so the test does not confuse Windows local timezone with PostgreSQL timestamptz UTC.
Docker commands have a 90-second timeout to avoid waiting indefinitely for a failed
Desktop engine. The runner does not repair/reconfigure Docker automatically.

Register a viewer through HTTP for JWT, read profile/post/reactions/comments/follow
list and both cold feeds. Imported safe IDs must exactly match feed IDs; unsafe
post stays in storage but is absent from feeds. Fixture image URLs are checked as
metadata; image bytes/frontend rendering and NGINX static asset serving are not
tested. Vector/NSFW services and benchmark seeding are disabled on test API.

Reports, snapshot, SQL, source and logs: `.tools/oasis-smoke/<runGUID>/`.
Each run has `run.json` with its unique Compose project `favi-oasis-<GUID>` and
whether Compose startup was attempted. `latest.json` is only the last completed
run summary; use per-run evidence for concurrent/failing runs. Process must exit 0.
Before up, verify fixed database/cache ports 15449/16399 are free. Parallel runs fail clearly
on busy ports rather than reusing another stack. A bind race still only cleans
the current run's unique project. Only a run that attempted up executes down,
including cleanup after partial up failure. Failing before up does not call Docker
cleanup. Existing application or other test projects are never targeted.
`test-isolation.ps1` creates a guarded real sentinel under the former fixed project
name, verifies missing-Python and busy-port failures leave container IDs unchanged,
then cleans only its own sentinel. It refuses to run if that legacy project exists.
The sentinel regression itself takes an exclusive local file lock to prevent
parallel invocations from sharing the legacy project. PostgreSQL health checks use
TCP (not the temporary initialization server's Unix socket); a bounded read-only
SELECT 1 from the host verifies readiness before database creation. No import
operation is retried to hide a data failure.
No push/merge/deploy is performed.
Default source is a synthetic fixture; -Simulator uses upstream engine output.
Neither mode uses LLM-generated autonomous actions. This is an adapter
integration test, not evaluation of graph recommendations, agents or accuracy.

Autonomous LLMAction is a separate runner: `run_autonomous.py <new-run-root>
.tools/oasis-upstream http://127.0.0.1:<ollama-port>`. Install Ollama qwen3:1.7b
locally, set OASIS_LOCAL_MODEL=qwen3:1.7b and use .tools/oasis-env/Scripts/python.exe.
Then run verify.ps1 -ExistingRunDirectory <new-run-root> -Python
.tools/oasis-env/Scripts/python.exe to import and verify that exact exported state.
Only HTTP loopback model endpoints are accepted. No provider keys are required.
Use an owned Ollama container with loopback port mapping; stop it after execution.
Full evidence and model quality limits: document/OASIS_SIMULATOR_VERIFICATION.md.

Hybrid small community: `run_community.py <new-run-root> .tools/oasis-upstream
http://127.0.0.1:<ollama-port>` uses 100 users, 20 ticks, seeded 90/9/1 roles,
homophily, bounded-confidence opinions, trust/fatigue and at most 12 native local
LLM calls for content. It calls public OASIS actions with policy-selected IDs;
this is explicitly hybrid ABM, not LLMAction autonomy. Import the resulting export
using verify.ps1 -ExistingRunDirectory as above. Raw audit + complete conversation
are in community-report.json, events.json, llm-calls.json and review.md.
See document/SMALL_SOCIAL_COMMUNITY.md for formulas, assumptions and evidence.

Self-authored society: `run_free_society.py <new-run-root> .tools/oasis-upstream
http://127.0.0.1:<ollama-port> --agents 6 --rounds 6` lets the local model choose
each agent's identity, emotions, goals, private experiences and actions. No assigned
topics, activity roles, starting follows/posts or minimum participation quota.
Wait/sleep and voluntary events are supported. Event announcements become posts;
event membership stays simulator sidecar state. Export uses the same existing
snapshot integration gate. See document/FREE_SOCIAL_AGENTS.md for boundaries.
Default local model is qwen3:4b; choose another installed model with --model.
All run harnesses are archived with hashes. No activity minimum or diversity
rewriting is applied; waiting and repeated content are preserved as outcomes.

Native full run: `python -X utf8 tools/oasis-smoke/run_oasis_native.py
<new-run-root> .tools/oasis-upstream http://127.0.0.1:<ollama-port>` uses the
original OASIS LLMAction/CAMEL memory loop, TwHIN-BERT, broad native social tools
and a local GPU model. Prepare the embedding cache using prepare_native_recsys.py
and create the model alias using native-8b.Modelfile first. Defaults reuse the
10 self-authored initial profiles from the preserved free-10x4 run; see --profiles.
The export is an explicit Favi subset; raw groups/reposts/reactions are preserved
in oasis.db and oasis-all-tables.json. Run review_native.py <run-root> to render
the complete public conversation and failure metrics, then verify.ps1 with
-ExistingRunDirectory. Protocol and interpretation: document/OASIS_NATIVE_FULL_RUN.md.

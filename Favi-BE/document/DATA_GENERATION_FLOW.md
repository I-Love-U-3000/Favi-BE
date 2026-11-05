# Default flow for recommendation and graph benchmark datasets

## Decision

Use controlled hybrid ABM behavior, a reusable synthetic text corpus, and the
public OASIS action API. This is the default **graph workload** generator, not a
simulation of autonomous people. Previous native8B run cost880,581 local tokens
for10people/40opportunities and still repeated content. The hybrid community run
needed3,171tokens for100people, but its4posts were insufficient for ranking
benchmarks. The new profile removes inference from the generation loop and makes
catalog size independent of LLM budget. No Ollama/GPU/API key is needed.

This is the best default for repeatability, speed and graph testing on this
project, not a measured universal winner. Caption realism is secondary here.
Native LLMAction and free-persona runners remain separate research tools. Do not
interpret synthetic text quality as real relevance or benchmark image search on
metadata-only image URLs.

## One command

Run from the solution directory (`Favi-BE/Favi-BE`):

```powershell
./tools/oasis-smoke/run-dataset.ps1
# Same flow, smaller integration dataset:
./tools/oasis-smoke/run-dataset.ps1 -Config tools/oasis-smoke/configs/smoke.json -VerifyFavi
# A different independent replicate:
./tools/oasis-smoke/run-dataset.ps1 -Seed 20261102
```

The script can be invoked by absolute path from another working directory; its
relative config/output/Python parameters are interpreted from the solution root.
Outputs default to a fresh `.tools/datasets/<GUID>`; override `-Output` with a
new directory. Existing directories are refused and never reset or overwritten.
To reuse a dataset without running OASIS or LLM again, verify its manifest first:

```powershell
python tools/oasis-smoke/check_dataset.py .tools/datasets/<existing-run-id>
```

This standard-library-only command checks all declared file hashes, rejects path
escapes/incomplete bundles and revalidates temporal history, config fingerprint
and quality. SHA256 checks detect accidental changes, not a signature proving
authorship. Retain the entire directory, not only CSVs, when sharing an experiment.
`-VerifyFavi` additionally runs isolated PostgreSQL/Redis/API verification; the
offline benchmark dataset does not require Docker. No production API/database is
written. The wrapper creates no persistent service.

## Prerequisites on a fresh Windows checkout

Python3.11, Git and uv are needed once. Current lock is Windows/CPU-specific.
From the solution directory:

```powershell
git clone https://github.com/camel-ai/oasis.git .tools/oasis-upstream
git -C .tools/oasis-upstream checkout ba892e0667eb84b312e4c0e5630f275154bbb98f
uv venv --python 3.11 .tools/oasis-env
uv pip install --python .tools/oasis-env/Scripts/python.exe --torch-backend cpu --constraint tools/oasis-smoke/requirements-oasis.lock.txt .tools/oasis-upstream
```

Do not rerun clone/venv creation over existing directories. Existing installations
are checked against42 upstream Python/SQL files and130 locked dependencies on
every run. Docker Desktop and .NET10 SDK are needed only for `-VerifyFavi`.
Keep `.tools` ignored; do not commit model caches, generated datasets or DB files.

## Configuration contract (version1)

`configs/benchmark.json`:1000users,300initial posts, up to500posts,80ticks,
8topics,5exposures per activation,50% uniform exploration. `configs/smoke.json`
uses100users/30initial posts/30ticks, max45posts. Copy either JSON to version a
new experiment; unknown/missing keys, invalid probabilities and unsupported
versions fail before engine startup. `--validate-config` validates without OASIS.

| Field | Meaning |
|---|---|
| schema_version | Exactly1; changes to field meaning require a new version |
| seed | Integer RNG seed; `-Seed` overrides it in resolved configuration |
| users |100..10000, multiple of100, exact90/9/1 reader/contributor/creator roles |
| initial_posts / max_posts | Explicit bootstrap catalog and total catalog cap |
| ticks |3..500 sequential rounds; a tick orders simulation, not real elapsed time |
| feed_size |1..20 distinct non-self posts per activation |
| exploration | Probability of uniform sampling for each visible slot |
| topics |2..32 unique topic labels; private preferences are Dirichlet-like Gamma(.6,1) weights |
| activity | Role-specific activation probabilities before fatigue |
| quality | Minimum likes, minimum warm users, and distinct likes required per warm user |

All fields are archived in config.resolved.json and hashed. Policy constants below
are versioned in code and archived with hashes; introducing new policy behavior
requires a new experiment configuration/revision, not editing old output.

## End-to-end generation and modeling assumptions

1. Validate config, create fresh output, verify pinned source/dependencies and
   archive harness; no runtime upstream patch or arbitrary SQLite inserts.
2. Seed exact90/9/1 roles and private preferences. OASIS registers users. Confirm
   user_id=agent_id mapping for this pinned runtime before using public actions.
   The scaled runner uses public SocialAction clients and Platform.running directly,
   including native sign_up and EXIT, without unused ChatAgent/StubModel objects.
   Earlier env.reset dispatch remains in archived historical harnesses.
3. Bootstrap2distinct homophily-weighted follows per user (baseline.1+affinity^3).
   Creators publish the configured initial catalog, round-robin creators/topics.
   Bootstrap events have tick=-1 and bootstrap=true. They are setup, not labels.
4. Each tick shuffles users using seeded RNG. Fatigue decays by.75; activation is
   role probability*(1-fatigue); activated fatigue rises.12 capped.8.
5. Feed slots sample without replacement: uniform exploration or weight
   (.05+topic_interest)*(1.3 if following author else1)*(1 if unseen else.2).
   Only already published, non-self items are eligible. This logging policy is
   independent of the algorithms subsequently benchmarked, but still biases data.
6. Record exposure before mutation. Creators can publish with probability.15.
   Otherwise sample a visible target weighted.05+interest; non-readers comment
   with probability.15 if not previously commented; then follow with probability
   .08*sociability if not followed; then like with probability.15+.65*interest if
   not already liked; otherwise read. Branches are sequential, not independent
   probabilities summing to1. Like/follow duplicates and self edges are prevented.
   Bounded-confidence opinion updates reuse the documented community policy;
   they do not establish psychological validity or determine ground-truth labels.
7. Await every public OASIS action and reject unsuccessful responses. No LLM
   calls, retries or fabricated interactions to meet quality thresholds.
   Inject DatasetChannel through the public Platform(channel=...) constructor.
   It preserves FIFO request queue, message IDs and original reply tuples, but
   replaces upstream100ms reply polling with asyncio futures (event-driven
   acknowledgement). The future is registered before enqueue, so an early reply
   cannot be lost. Each waiter has45s timeout and cancellation cleanup; duplicate
   replies fail. EXIT requires no reply. Engine writes remain sequential and
   unmodified; this optimizes transport rather than bypassing OASIS validation.
   Default generation has a30minute timeout; `-TimeoutSeconds` can explicitly
   extend it up to4hours for larger provisioning. Harness archives the channel source.
   Mutations call the original public SocialAction methods bound to each actor ID,
   avoiding SocialAgent.perform_action_by_data rebuilding all tool schemas and
   retaining unused LLM memory on every action. Registration uses native sign_up
   in ascending actor order; OASIS owns table definitions, data writes and source trace.
   The larger profiles add declared nonunique lookup indexes before registration,
   as specified in [FAVI_DATASET_SCALES.md](FAVI_DATASET_SCALES.md). This is
   a dispatch optimization, not a change to behavior policy or source validation.
8. Close engine; validate all logical FK links and known physical DDL diagnostics,
   temporal event order, prior exposures, unique positives and exact graph/catalog/
   comment equality against final SQLite. Enforce quality thresholds before export.
   The final catalog must also stay between initial_posts and max_posts.
9. Original adapter validates timestamps/references/duplicates/self-follow and
   publishes JSON/SQL/graph CSV. Source hash must remain unchanged. Finish with
   dataset-report.json and SHA256 dataset-manifest.json. Failures retain evidence
   and failure.json; absence of a final manifest means incomplete. Optionally run
   PostgreSQL replay/rollback and localhost HTTP verification using the same export.
   The integration gate reads all pages of followers/comments/feed and checks
   advertised totals. Every imported post is verified through its detail API.
   Small catalogs (<=60safe posts) must appear completely in both feeds. Larger
   catalogs require a nonempty, unique, safe subset because production feed
   candidate pools are bounded; no assumption that feed equals a catalog dump.

## Artifact and temporal evaluation contract

`events.jsonl` is the authoritative history: monotonic0-based seq and tick, actor,
kind and bootstrap flag. Posts carry source post_id/topic/content; exposures carry
ordered post_ids; like/comment carry exposure_seq pointing to a preceding exposure
by the same actor in the same tick; follows carry target_user_id. Sequence gives
strict ordering when multiple events share a tick. Read-only turns are represented
by exposures without a subsequent positive. **These are not verified negatives.**

`oasis.db` and `export/snapshot.json` are final state, **not training data at an
earlier cutoff**. `export/users.csv`, `posts.csv`, user_post_edges.csv and
user_user_edges.csv map source IDs to Favi UUIDs for graph loaders. Use the
snapshot only for integration/final-state inspection. For offline evaluation,
choose global tick cutoffs, build each graph from earlier events only, include
items published before query time, exclude past positive edges, and tune only on
validation. Do not use final popularity/follows or private latent preferences as
ranking features. Latent preferences are simulation state, not observed labels.
Comments/follows are distinct signals; do not silently relabel them as likes.

Generation itself does not train models or select evaluation cutoffs. The separate
[offline evaluator](RECOMMENDER_BENCHMARK.md) now measures Popularity and the
existing Favi formula with a fixed temporal protocol; it does not prove that
LightGCN will beat Popularity. Warm-user count is based on the whole run; actual
train/test eligible counts must be measured after splitting. Keep several seeds
and report synthetic-policy bias when comparing algorithms. Same config/seed/
harness reproduces semantic event bytes; wall-clock source timestamps, UUID
namespace/output path and duration can differ. Confirm event SHA256 on reruns.

## Verification evidence — 01/11/2026

Provisioning profile `configs/favi-5k.json` targets5000users,1500initial posts,
max2000posts,500ticks and at least100000distinct likes/4000users with5likes.
Reader activity is explicitly.16 (other profiles remain.12); no extra actions are
fabricated to meet the gate. Large output is stored under `.tools/datasets` with
raw SQLite, event history, Favi snapshot/SQL/CSV and integrity manifest. Run with
`-Config tools/oasis-smoke/configs/favi-5k.json -TimeoutSeconds 7200`.

Default profile run `.tools/datasets/benchmark-default-20261101`:

| Measurement | Observed result |
|---|---:|
| Users |1000 (900readers,90contributors,10creators) |
| Posts |374 (300bootstrap +74later) |
| Likes |3660 distinct user–post edges |
| Follows |2511 (2000bootstrap +511later) |
| Comments |644 |
| Activation exposures |12678 (each carries5visible item IDs) |
| Total ordered events |19867 |
| Users with at least3distinct likes |605 over the full run |
| Inference requests/tokens |0 /0 |
| Generation wall time |259.406seconds, about4minutes19seconds |

These measurements include source/dependency verification, population construction,
OASIS writes, audit and export, excluding PostgreSQL/API verification. They are
single local observations under concurrent smoke execution, not latency SLAs.
Event SHA256: a7b89fc23aec1f3508eab413f6998895c1d89b495659a10449a8cc7a15565ba0.
The bundle checker verified all22declared files and temporal quality checks.

Smoke profile produces100users/31posts/125likes/219follows/26comments,858events,
457activation exposures and10warm users. The original polling transport took
66.688seconds; future-based transport took29.281seconds with the **same exact
event SHA256**, 7df76a183703caaa510b8089c9d627604189cf7b453cc7484fbe9e36099c970e.
An explicit same-seed rerun produced identical event bytes again. Determinism here
means this observed policy/event contract, not identical native timestamps/SQL.
Final smoke `.tools/datasets/standard-release-20261101` completed in24.672seconds
when invoked from the repository parent, retained that exact event hash, passed
the22-file bundle check, and refused an attempted overwrite without changing its
manifest. Earlier Windows manifests used backslashes; checker normalizes them,
while new manifests write portable forward-slash paths.

Full default dataset integration run
`.tools/oasis-smoke/39212425c4564363be0be5855b2e5412` passed19982PostgreSQL/import/
HTTP checks, including replay unchanged, injected FK rollback, every imported
entity and paged follower/comment/feed contracts. An earlier attempt stopped on
the legacy single-page bound; that assertion was removed when adding paging and
the full dataset was retested. Small-catalog paging gate also passed1543checks.
65unit tests pass (12adapter +53smoke, including15new dataset tests). Existing
NU1903 SignalR.Common9.0.11 warning remains; this is offline/integration readiness,
not production release approval. All integration-owned Compose resources cleaned
up; no model service was started for this flow. Raw generated files remain ignored.
# Adaptive onboarding preparation — 05/11/2026

Lifecycle-v2 pilot implementation: `--adaptive --arrival-policy lifecycle-v2`
allocates 30% bootstrap, 15% immediately before each cutoff, and 20% before
each cutoff at 10/30/60-tick offsets. Topic count cycles by `(user // 3) % 3`
to avoid perfectly confounding topic count with the offset group `user % 3`.
This does not guarantee every finite stratum is exactly balanced.
Creators are not forcibly moved: bootstrap creator presence is fail-closed
and role distributions require inspection before confirmatory freeze.

Main onboarding uses initial latent-interest top topics as synthetic informative
self-report. All modes are archived as `onboarding.informative/noisy/independent.json`;
noisy replaces ceil(n/2) slots (one of one or two of three), explicitly rounding
the nominal 50% corruption rate upward for small lists. Initial latent state
is generator provenance only. Declared preferences do not affect exposure in
this version, so alternate sidecars support ranking replay, not behavioral effects.

`--scenario sparse-topic` maps nonempty actual preferences to music and caps
music at nine posts; subsequent music post intentions become travel. This
deliberately changes content distribution. Alternate preference sidecars remain
pre-scarcity provenance and must not be interpreted as sparse actual preferences.
Analyzer independently recomputes onboarding and verifies the cap. Main requires
C0/C1/C2/T1/T2/W1/W2; sparse requires C3; other cells are explicitly N/A for
gate purposes but counts remain visible. Combined readiness requires both
scenario gates. Tags are controlled simulator topic metadata, not imported
production PostTags; API parity remains a separate gate.

Pilot config: `configs/adaptive-lifecycle-v2.json`, 5.000 users/2.000 post cap/
500 ticks. Source changes during a run do not alter its archived harness;
record analyzer source hash separately from generation harness hashes.

Cold expansion uses `--adaptive --arrival-policy cutoff-waves-v1`: 40% user IDs
start at -1, 30% at validation cutoff minus one tick, and 30% at test cutoff
minus one tick. Within each wave, topic counts cycle 0/1/3 independently of
labels. One tick of natural activity is allowed before classification; users
are not forcibly kept cold and likes are not deleted. This is a controlled
stress population, not a claim about real arrival rates. The selected policy
is saved in manifest-hashed `adaptive-policy.json`; read-back cohort analysis
recomputes the plan and requires equality. Default uniform arrival remains
available. Run all policy comparisons on pilot data before freezing protocol.

Follow-up: `generate_dataset.py --adaptive` now writes `onboarding.json`, delays
actor activity until logical arrival and limits bootstrap follows/posts to
bootstrap participants. Native accounts still exist from bootstrap. Use
`configs/adaptive-pilot-long.json` for the 1.000-user/500-tick exploratory pilot.
`analyze_adaptive_cohorts.py --dataset <bundle> --output <new-report.json>` first
verifies the complete manifest and independently audits arrival constraints.
It uses 60/20/20 chronological windows, excludes own/prior-positive/future items,
and reports each cohort's total users, users with/without targets, distinct
targets and excluded targets. C3 takes priority over C1/C2 when fewer than ten
eligible items match declared topics. All eight cohorts require at least thirty
target-bearing users per phase for the pilot gate. Missing cohorts fail the
gate rather than disappearing from the table. Output must be a new path.
This audit does not claim production tag import or cold-ranker parity.

`tools/oasis-smoke/adaptive_onboarding.py` defines a separate deterministic
arrival/preference plan for the forthcoming adaptive dataset. It uses the ten
production topics; a regression compares them with `InterestTopics.cs`. The
first half of user IDs arrive at bootstrap tick -1, the rest uniformly during
the simulation. Declared topic counts are sampled from 0/1/3 independently of
latent interests. This deliberately models uninformative onboarding as a stress
condition, not accurate self-reporting or a natural population distribution.
The current policy has no preference changes after arrival.

Validation requires exactly one row per user, valid topic membership, unique
topics and ordered integer arrival/preference timestamps. At request cutoff t,
only arrivals and preferences strictly before t are visible; an unknown user
returns None, while an existing user without visible preferences returns [].
The component does not yet delay native OASIS actions, change event/export
schemas, or guarantee sufficient target-bearing cohorts. Those integration
gates must pass before generating the confirmatory datasets. Four unit tests
passed on 05/11/2026, including production catalog parity and temporal leakage
boundaries. Historical generation behavior below remains version 1.

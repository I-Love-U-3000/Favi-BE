# Handover: Favi adaptive research

## Protocol design checkpoint — 08/11/2026

Protocol/search design is now frozen as adaptive-v1-20261108 in
benchmarks/adaptive-protocol-v1.md and
tools/oasis-smoke/configs/recommenders/adaptive-v1.json (relative to solution).
Related work table: benchmarks/adaptive-related-work.md.
Working-file config SHA256 4a417a1c2f2adb6e75159306464774d22b987c58d6715a946d6a7ccc5ad3be46;
Git newline normalization may change checkout bytes: hash actual archived bytes.
JSON parsed and grid counts checked with PowerShell; git diff --check PASS.
This milestone changes design/docs/config only, no numerical tests needed.

NEXT SESSION: implement runner according to this protocol, not old expanded
selection. No confirmatory dataset or ranking run occurred in this session.
Execution gate NOT frozen/PASS yet: runner/source/lock inventory and meaningful
replay tests must finish before confirmatory data/benchmark. Read latest protocol
fully, especially joint threshold/model/config/epoch selection, tolerance tie
relative to global max, dual-threshold LightGCN patience/checkpoints, V2 historical
zero-tag definition and main-only primary aggregation. V2 stays in scope with
12 configs and same cold fallback at threshold1. No silent omission.

No active child jobs. Lease owner protocol-freeze-session will mark handoff-ready.
Git status before this milestone was only preexisting root nul. No PR/push/merge
or deployment. Continue using fresh session per user instruction each milestone;
no subagents. Confirmatory seeds20261111/12/13 remain ungenerated/unobserved.
The prior checkpoint below describes completed code/pilot evidence and remains
valid except that protocol design/grid/related-work gap is now addressed.


## Latest checkpoint — 08/11/2026

Primitive parity committed 6c2083e. Offline cold pool adapter/evaluator pilot
PASS, committed 82f4fb4. Read ADAPTIVE_OFFLINE_CONTRACT.md and
benchmarks/ADAPTIVE_BASELINE_PILOT_20261108.md. Main/sparse pilot baseline runs
completed: candidate/target fingerprints and per-cohort denominators match.
101 full Python tests plus subsequently added bounded-pool test passed (4
adapter tests). No Python/.NET job currently active. NEXT: freeze protocol/grid
and implement collaborative/router runner; confirmatory seeds still ungenerated.
Older pending-reader notes below are superseded by this checkpoint.
Lease solution `.tools/adaptive-worker.json`: update each checkpoint, mark
handoff-ready before a fresh session. Scheduled 02:00 task must avoid overlap.

Fresh-session starting action: inspect git status (expected only root nul),
claim worker lease, read configs/recommenders/expanded.json and multiseed docs,
then freeze statistical protocol separately from implementation debugging.
Do not rerun completed parity/pilot without a new concern. Build collaborative
runner on InteractionData/fit_ease/fit_ppr/fit_lightgcn using AdaptiveFeed wrapper;
keep same cutoff/candidates/targets and cold fallback. Selection sees validation
only; save/hash choices before test. Existing expanded runner's tuning used
likes-only rows without arrival-aware filtering: don't blindly reuse its score
selection as the new adaptive system selection. Declare warm model search and
threshold search budget explicitly; keep optimizer-seed hierarchy in reporting.
Formula V2 was in original proposed scope but new contract runner doesn't yet
support it: either implement with fixed validation grid or document a protocol
scope revision before confirmatory test. Do not silently omit it.
Confirmatory data seeds 20261111/12/13 still unused; generator frozen at 3bb48b3
except helper classify_cohort added later (generation behavior unchanged).
Both pilot baseline tests were observed: exploratory only. No PR/push yet;
user permits commit-per-milestone, so lack of PR does not block progress.

Updated 07/11/2026 Asia/Saigon. Read root and nested AGENTS.md first.

## User authorization

Continue milestones without routine review, each as a separate tested commit/PR.
Vietnamese reports must explain outcome, evidence, limits and next action.
Use Sol Low for building, at most Medium analysis; no claim to control settings
without a supported tool. User explicitly requests resumption around 02:00 on
08/11/2026 after quota reset, then work during that quota window and stop when
quota is exhausted; do not schedule further reset cycles. Quota exhaustion is
not introspectable: save checkpoints frequently, do not promise detection.
User permits a fresh session using this file when context is insufficient.

## Repository and evidence

Root C:/Users/tophu/source/repos/Favi-BE; solution nested Favi-BE.
Branch codex/adaptive-recommender-v1. Last completed checkpoint 3bb48b3.
Untracked root `nul` predates work: preserve it. Never reset others' work.
Pilot main/sparse each 5k users/2k posts/500 ticks pass all required cohort
target-user counts at both cutoffs. 98 tests passed. Read
document/benchmarks/adaptive-lifecycle-v2-20261105/REPORT_VI.md and report.json.
Raw datasets and verified ZIPs in .tools; do not put raw data in Git.
This is synthetic data readiness, NOT evidence of ranking improvement.

## Current milestone: baseline parity primitives PASS, reader pool pending

Read ADAPTIVE_FINAL_REVISION.md, adaptive-protocol-v1.md, COLD_START_FEED.md,
RECOMMENDER_BENCHMARK.md and source before edits. Production formula includes
tag-count bonus and stable input-order ties; legacy Python benchmark deliberately
has zero tags and ID ties under a different offline candidate contract. Do not
rewrite old results or call retrieval/authorization equivalent to score parity.
`favi_rankers.py` and `run_ranker_parity.py` passed 524 checks (20 warm/25 cold)
against linked/extracted actual C# ranking. 13 .NET ColdStart tests pass. Read
document/benchmarks/BASELINE_PARITY_20261107.md. Next build cold reader pool
adapter and freeze protocol, preserving the legacy benchmark contract. Compare
scores/tie behavior, author damping, tag cap, follow/mutual/self, ages and cold
round robin/dedup/cap relaxation/fallback. Exercise GUID order boundary cases.
Existing ColdStartReaderTests cover broader DB behavior; don't claim a pure
ranker fixture covers SQL, permissions, hydration or routing.

## Milestone sequence

1. Baseline parity report and meaningful tests. Commit separately, record hash.
2. Freeze protocol/search grids/input-signal/tag contract after parity; commit.
3. Generate main+sparse confirmatory seeds 20261111/12/13 if still unobserved;
   verify all gates and ZIPs, keep failed seeds, record source/config hashes.
4. Implement/run common-candidate adaptive benchmark with validation-only
   tuning, per-cohort metrics, robustness sidecars, cost and negative findings.
5. Verify actual HTTP lifecycle/fallback/privacy and resource limits; report.

After EACH milestone update this file with exact commit, status, commands,
results, limitations, pending work and process IDs. PRs when repository access
is available; at least separate commits as authorized. No auto merge without
applicable checks. `gh` absent; discover GitHub connector capabilities instead
of installing blindly. No Git remote state changed. One-off automation
`favi-ti-p-t-c-sau-reset-02-00` resumes at 08/11/2026 02:00 Asia/Saigon with
this file on Sol Low. Do not create further reset cycles. User clarified to
work NOW too; the 02:00 run is continuation if interrupted, not a reason to wait.

## Runtime

Use PowerShell login:false. Python .tools/oasis-env/Scripts/python.exe in solution.
.NET/Docker/numeric work outside sandbox via approved require_escalated CLI;
sandbox DLL failures previously caused Windows popups. Never repeat that path.
No subagents unless explicitly requested. No UI or filesystem cleanup of
unrelated resources. Before resuming, inspect git and active processes to avoid
overlap with an earlier worker. Current parity milestone has no background run.

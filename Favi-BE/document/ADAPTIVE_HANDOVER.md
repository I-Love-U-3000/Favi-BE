# Updated work-window authorization — 08/11/2026

User bổ sung trong chat generation: sau phiên 02:00–07:00 tiếp tục làm luôn
đến hết phiên 07:00–12:00 ngày 08/11/2026, giờ Asia/Saigon. Đây là cập nhật
cho giới hạn một quota window bên dưới: tiếp tục cả hai phiên đã cho phép,
không tự tạo thêm chu kỳ reset. Vẫn commit từng milestone, lưu handover và
chuyển chat mới giữa các mốc; không subagents hoặc dừng chờ review thường lệ.

Automation mới đã tạo theo yêu cầu user: `favi-ti-p-t-c-07-00-n-12-00`,
07:00 một lần ngày 08/11/2026, Sol Low, ACTIVE lúc tạo. Tiếp progress mới nhất
từ handover, kiểm lease/process trước nhận việc, không overlap: worker còn chạy
thì automation kết thúc thay vì mở batch khác. Không reset quota. Automation
02:00 cũ vẫn giữ nguyên; chưa đến giờ chạy khi kiểm lúc 00:58 ngày 08/11.

# Confirmatory generation checkpoint — 08/11/2026

Milestone hoàn tất: cả main và sparse-topic seeds 20261111/20261112/20261113
đã sinh và PASS integrity/policy/cohort tại cutoff300/400 cùng ZIP readback.
Không thay seed, hạ gate, sửa generator hoặc chạy ranking/tuning model. Không
có failed run trong batch. Mỗi bundle 5.000users/2.000posts/500ticks; mỗi ZIP
32members được đọc lại SHA256. Minimum required cell208target users, gate30.

Báo cáo: benchmarks/adaptive-confirmatory-data-20261108/REPORT_VI.md cùng
generation.json và12gate/archive JSON. Source commit khi sinh: 2ea65f8;
milestone commit chứa checkpoint này có title `docs(research): qualify six
confirmatory adaptive datasets`; lấy exact hash bằng git log/lease sau commit.
Source/config/lock/CLI inventory và130dependencies trong generation.json.
Source ZIP và script batch freeze ở solution
.tools/benchmarks/adaptive-generation-20261108/frozen-source.zip; raw/logs ở
.tools/datasets/adaptive-confirmatory-{main|sparse}-{seed} và
.tools/benchmarks/adaptive-generation-20261108. ZIP/receipts ở
.tools/dataset-archives/adaptive-confirmatory-{main|sparse}-{seed}.zip[.json].
Batch CLI: `.tools/oasis-env/Scripts/python.exe -X utf8
.tools/benchmarks/adaptive-generation-20261108/generate_batch.py` từ solution;
không rerun script trên outputs cũ. Generation elapsed từng dataset509.594–562s;
đây không phải model/HTTP benchmark. Source drift check sau mỗi dataset PASS.
Không rerun111tests hoặc parity/pilot: source không đổi, gate thực tế đủ6lượt.

NEXT CHAT: kiểm Git/process/lease rồi claim lease; đọc protocol/config frozen,
ADAPTIVE_RUNNER_20261108.md và report dataset mới. Kiểm hash/source inventories,
dataset manifests và receipts trước dùng. Chạy run_adaptive_benchmark.py tuần
tự trên cả6qualified datasets với output mới; giữ mọi lỗi/log, dừng batch khi
fail. Lưu frozen selection validation-only trước test, exact-epoch refit,
optimizer seeds20261103/04/05; không retune theo test. Sau đó báo riêng main
và sparse; main-only practical gate, robustness/ablations, optimizer hierarchy,
cost/HTTP còn phải hoàn thành, có thể chia milestone kế tiếp. Không claim
superiority từ data gate. Data cohorts đã quan sát nhưng ranking chưa quan sát.

Batch process session83140 đã kết thúc exit0, không Python/.NET jobs còn chạy
khi kiểm cuối. Lease chuyển handoff-ready trước chat mới. Root nul cũ giữ nguyên.
Không push/PR/merge/deploy. Giữ work-window/automation no-overlap ở phần trên;
không subagents; numerical CLI require_escalated để tránh sandbox DLL popups.
Các checkpoint ungenerated bên dưới là lịch sử đã được mốc này thay thế.

# Runner checkpoint — 08/11/2026

Common-candidate adaptive runner milestone implemented; commit with this heading.
Read benchmarks/ADAPTIVE_RUNNER_20261108.md and current frozen protocol in full.
111 Python tests PASS, including9 new runner tests; real numerical fixture uses
EASE/PPR/LightGCN, orchestration fixture mocks data gate/lock/tuning explicitly.
Dependency lock130packages matched; source inventory16files and source ZIP
readback PASS: .tools/benchmarks/adaptive-runner-readiness-20261108/readiness.json.
ZIP SHA256 5142e8a93fcdd8a0d1b17774822dc0cd7f8cb8b01f5b1c4e946bc0e3c50a6827.

NEXT SESSION: claim lease after git/process check; generate confirmatory main
and sparse-topic datasets seeds20261111/12/13 (still ungenerated/unobserved).
Read generator/docs/previous pilot commands before executing. Runner/source/lock
readiness now PASS; individual dataset gates/archive ZIP readback still mandatory.
Run no confirmatory benchmark until all six dataset gates pass. Keep failed
seeds/logs; stop batch if gate fail, no seed substitution or lowering cohort gates.
Record frozen source/config/lock inventories and CLI for dataset generation.
Then run tools/oasis-smoke/run_adaptive_benchmark.py on each qualified dataset.
Runner reports selected B1/R, family threshold1, V2, Popularity/topic-only/V1.
Robustness sidecars/ablations, cross-dataset main-only primary practical gate,
optimizer hierarchy report, full cost/HTTP serving remain subsequent work.
No claim of recommendation improvement has been made. No confirmatory data or
ranking generated in runner session. Existing pilot baseline/parity not rerun.
Git preexisting root nul preserved. No background Python/.NET jobs after tests.
No PR/push/merge/deploy. Continue milestone-per-commit with fresh sessions as
user requested, no subagents, no additional quota reset cycles. Numerical CLI
require_escalated to avoid sandbox DLL popups. Preserve all prior limitations.
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

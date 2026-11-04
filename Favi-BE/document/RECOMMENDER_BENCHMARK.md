# Offline Popularity baseline and current Favi formula

The separate [Formula V2 experiment](FORMULA_V2_BENCHMARK.md) searches a small
validation-only grid for social weight, decay and an optional hard author cap.
The V1 port below remains fixed. New reports also include author diversity and
max-author share; historical artifacts retain their original metric schema.

## Scope and protocol version1

Run `tools/oasis-smoke/benchmark_recommenders.py` with a verified generated
dataset and `configs/recommenders/popularity.json`. Python standard library only;
no model server, OASIS execution, PostgreSQL or production changes. Check all
dataset hashes and its temporal validation before evaluating. Write to a fresh
output directory; a finished run has manifest.json. Raw per-user predictions and
targets are retained for paired comparison by later algorithms.

Use `--methods popularity` to run and report only Popularity. Omit the flag to
compare both supported methods. Each selected method uses the same temporal
protocol; population fingerprints are checked within each evaluation window.

```powershell
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/benchmark_recommenders.py --dataset .tools/datasets/benchmark-default-20261101 --output .tools/benchmarks/popularity-001
```

Global tick split: train60%, validation20%, test20%, floors at boundaries. For80
ticks, training is[0,48), validation[48,64), test[64,80). Bootstrap tick=-1 catalog
and follows are observable setup. Validation model uses events strictly before48;
test refits on train+validation strictly before64. No test likes, comments, follow
edges or future-created posts enter fitting. No parameter is selected using test.
Current weights are fixed from source, not optimized against this dataset.

This is **frozen-window catalog evaluation**: recommend once per user at the
window start and recover distinct likes during that window. Candidates are all
posts already published at the cutoff, excluding own posts and prior likes. They
are the same for both methods; no sampled negatives. Later-created items cannot
be recommended in this protocol; their positive edges and users left with no
eligible target are explicitly counted. Users with eligible held-out targets
are macro-averaged; warm means at least1prior like, cold means0. Users without
targets are not assigned zero relevance. Full-run warm counts from generation
are not used to select evaluation users or features.

Per-user candidate-ID fingerprints and target IDs must match between methods;
the runner checks the combined evaluation-population fingerprint before reporting.
Popularity does not allocate comment/follow/author-affinity feature state. The
formula does, because those signals are part of its score.

## Popularity

score(item)=number of distinct observed likes before cutoff. Dataset validation
already prohibits repeated user–item likes. Retain zero-like items with score0;
sort descending score, ties ascending source post_id. Precompute global order;
filter self/seen per query. No comments, follows, latent preferences, impressions,
future aggregate counters or timestamps are used as features.

## Existing formula audit

Source: Favi-BE.API/Application/ContentDiscovery/ContentDiscoveryQueryReaderAdapter.cs,
GetNewsFeedAsync (warm authenticated feed), not guest/explore/cold-start ranker.

Base score = [2.5*A +1.5*log10(1+1.1*reactions+2*comments)+T] * exp(-.05*ageHours).
A starts1, self adds1; otherwise followed author adds1.5 and mutual adds.5;
recent30-day author reactions add min(2,.4*count). T=min(1.5,.3*tagCount).
Sort base score; per-author first item multiplier1, second.65, subsequent.4;
sort adjusted scores again. This is a hand-weighted heuristic, not learned weights.

Findings from code:

- The comment “Topic/Tag Overlap” is misleading: T counts tags, not overlap with
  user interests. More tags receive a bonus even if irrelevant to this viewer.
- Half-life is ln(2)/.05≈13.86hours. Without a meaningful simulated clock, using
  wall-clock generation duration would make decay practically constant.
- Author damping limits scores, not number of same-author recommendations; it
  does not guarantee top-K author diversity or measure user-specific fatigue.
- The comment “70%network/30%discovery” describes limits200and100, nominally2:1,
  not an enforced output ratio. Overlap/filtering changes the realized mix.
- This formula is bypassed by the separate cold-start branch where applicable.

The benchmark ports the ranking math while enforcing the common offline candidate
contract. Source SHA256 is recorded. It does **not** replay production SQL candidate
retrieval (200network/100discovery), moderation/privacy, hydration, pagination or
the cold-start branch. Stable source-ID ties replace unspecified source tie order.

Cold-user formula results are counterfactual diagnostics: in the real application,
users with no Like/Love/save go through the cold-start ranker. Use the reported
warm cohort when interpreting this warm-feed formula for its intended users.
Dataset import creates no PostTags: T=0; simulator topic/preferences are **not**
silently substituted for observed tags. All synthetic posts are public/eligible.
Production allows own/already-liked candidates; this evaluation excludes them for
both methods. Likes are the only available reaction type. Comment and follow
features come exclusively from pre-cutoff events, including bootstrap follows.

`hours_per_tick=1` is an explicit virtual-time scenario, not recovered real event
time. Set0 to disable decay for sensitivity analysis; use validation to decide
future deployment weights. A time-scale sensitivity run is a separate protocol,
not permission to choose whichever test result looks best.

## Metrics and measurement

Binary relevance means recorded future positive recovery, not human ground truth.
Recall@K=hits/eligible held-out positives; precision@K=hits/K; hit rate indicates
at least1hit. DCG=sum(1/log2(rank+1)) for relevant ranks1..K; nDCG divides by the
ideal DCG for min(K,positive_count). Macro-average each metric across evaluable
users and report warm/cold separately. Coverage=unique recommended items divided
by all cutoff-catalog items. Uniform random expected Recall@K=min(K,candidate_count)
/candidate_count is an analytical chance reference, not another trained model.

Fit wall time is measured with tracemalloc active and includes temporal state
construction. Peak is **Python tracked allocation**, not process RSS/native RAM.
Ranking latency measures warmed Python rank calls, repeated5times per evaluable
user, excluding fitting, auditing, JSON/I/O and tracemalloc. Report median/p95 with
nearest-rank p95; these are offline kernel timings, not HTTP SLAs or load tests.

Single synthetic seed cannot establish statistical superiority or real-user
relevance. Logging-policy exposure bias remains; non-liked/unobserved candidates
are not verified negatives. Report candidate/label coverage and time-scale effects
before deciding whether the formula merits production changes. Original dataset
and application code remain unchanged by the benchmark.

## Results and decision — 01/11/2026

Dataset:1000users,374final posts,3660likes; event fingerprint
a7b89fc23aec1f3508eab413f6998895c1d89b495659a10449a8cc7a15565ba0.
At test cutoff64:355available posts and2978past likes. Test has456users with682
likes;15likes target later-created items, leaving448evaluable users (417warm,
31cold);8users have no eligible target. Validation has503evaluable users.

With the declared1hour/tick scenario, test results:

| Method | All Recall@10 | All nDCG@10 | Warm Recall@10 | Warm nDCG@10 | Catalog coverage |
|---|---:|---:|---:|---:|---:|
| Popularity |0.022879 |0.011188 |0.022182 |0.011165 |0.036620 |
| Existing formula port |0.028646 |0.013791 |0.025180 |0.011681 |0.056338 |

Analytical uniform-random expected recall for the same all-user candidate sets is
0.028523. The formula's all-user result is very close to this reference. Warm
recall improves only about0.30percentage points, with no multi-seed/significance
claim. Cold counterfactual results account for part of the apparent all-user gain.
Validation recall is0.031809 Popularity vs0.039066 formula; warm validation recall
is0.033872 vs0.044226. No settings were selected on test.

Sensitivity protocol hours_per_tick=0 (decay disabled) gives formula all-user test
Recall@10=0.016257 and nDCG@10=0.009882, below Popularity. This diagnostic was
declared before execution and is not a tuned production recommendation. The
comparison is sensitive to the clock assumption, so no real-time effectiveness
conclusion follows from simulated ticks.

Release run `.tools/benchmarks/popularity-formula-release-20261101`:
Popularity fit11.1901ms/500.7KiB Python allocation peak; formula fit16.4873ms/
877.9KiB. Warmed ranking p95 is0.0362ms vs1.0489ms respectively (2240timed queries
per method). Previous repeats varied, especially formula p95 around0.58–1.05ms;
these are Python-port kernel observations, not measurements of the C# API.
Main, repeated, optimized and release runs reproduced the same quality metrics.
Sensitivity evidence: `.tools/benchmarks/popularity-formula-release-nodecay-20261101`.
Each run contains report.json, review.md, protocol.json, per-user JSONL, archived
harness and a file-hash manifest. No dataset/application mutation occurred.

Decision: keep Popularity as the inexpensive reference and the existing formula
as a separate heuristic baseline. Do not replace production ranking or claim the
formula validated on this evidence. Before tuning it, define a meaningful clock,
use actual user–tag matching if desired, and evaluate more independent datasets
with the same candidate contract. PPR is the next graph baseline.73unit tests pass
(12adapter +61smoke), including8new tests for temporal leakage, metric arithmetic,
self/seen filtering, formula affinity/damping, time decay and catalog availability.

Popularity-only run `.tools/benchmarks/popularity-only-20261101` used
`--methods popularity`: test448users, Recall@10=0.022879, nDCG@10=0.011188,
coverage0.036620, warmed rank p95=0.0300ms. It matches the Popularity quality
metrics from the comparison runs and does not fit/evaluate the formula. The8
ranking regression tests also pass after adding method selection.

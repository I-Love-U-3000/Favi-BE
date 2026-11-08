# Formula V2 offline tuning and benchmark

The subsequent [three-scale benchmark](THREE_SCALE_RECOMMENDER_BENCHMARK.md)
repeats this search independently on 2k, 5k and 10k datasets and reports the
accuracy/author-concentration tradeoff for each selected configuration.

## Scope

Compare Popularity, unchanged Favi warm-feed Formula V1, and a validation-tuned
Formula V2 on immutable `favi-5k-v1`. This experiment changes offline research
tools only, not production code, SQL candidates, DI, endpoints or dataset files.
Use the temporal/candidate/label contract in RECOMMENDER_BENCHMARK.md. All methods
share catalog, self/seen exclusions and per-user candidate/target fingerprints.
V1/V2 have comment/follow features; Popularity uses only likes, so this is a
comparison of solutions with different signals, not an isolated algorithm test.

## V2 formula

Retain V1 social affinity A, quality Q=log10(1+1.1*likes+2*comments), and the
30-day history definition. Score=(social_weight*A+1.5*Q)*exp(-decay*ageHours).
All aggregates and follows come from events strictly before the query cutoff.
No tags, latent simulator preferences or exposure corrections are introduced.
The assumed clock remains1hour/tick; this is a simulated-clock experiment.

V2 sorts scores descending, ties ascending source post_id. It either returns topK
directly or greedily accepts at most2items/author until K. It does not silently
relax the cap when fewer items are available. V1 keeps its original soft1/.65/.4
author damping; V2 uses the explicit cap or no diversity adjustment instead.

Search is fixed before test evaluation: social_weight in[.5,1.5,2.5], decay
in[0,.005], author_cap in[null,2], total12configurations. Quality coefficients are
held fixed, so the searched social coefficient represents a relative weighting.
This is a small finite search, not unrestricted formula discovery.

## Selection and evaluation

On500ticks: fit prefix<300; validation likes[300,400). The selection function
receives only events with tick<400, never test labels. Choose largest macro
nDCG@10, then Recall@10, then earliest grid position for deterministic ties.
Archive protocol, tuning prefix hash and every trial's validation result.
Persist selection.json before any test evaluation and check its hash unchanged
after evaluation. Refit chosen parameters on prefix<400; test window[400,500).
Popularity and V1 are fixed baselines, not tuned with test results.

Each baseline and selected V2 is evaluated on validation and test, including full
per-user predictions/targets. Timing is disabled for search trials, enabled for
the final models under the existing protocol. Fit time uses tracemalloc; report
Python allocation peak separately from warmed ranking median/p95. These are not
C# API load measurements or total RAM consumption.

Author diversity=distinct recommended authors/returned items; max-author share
is the largest author's fraction of returned items. Both are0for an empty slate.
These describe output concentration, not usefulness or fairness guarantees.
Report these with Recall/nDCG, coverage and latency so a cap cannot appear to
improve recommendations solely by making the slate more diverse.

Paired user bootstrap resamples the same test-user deltas with replacement2000
times, fixed seed20261103, and reports the2.5/97.5percentile interval for mean V2
minus baseline Recall/nDCG. It preserves pairing but does not measure uncertainty
across generated dataset seeds, and does not correct all possible tuning bias.
A validation win or a single-dataset interval is not production validation.
Users share graph structure, so a user-resampling interval may also be optimistic
when their outcomes are dependent; it is an exploratory within-dataset diagnostic.

## Run and artifacts

```powershell
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_formula_benchmark.py --dataset .tools/datasets/favi-5k-v1 --output .tools/benchmarks/formula-v2-favi5k-001
```

Require a fresh output path. Verify dataset integrity first; retain incomplete
artifacts on error. A finished run has manifest.json with hashes for protocol,
tuning.json, selection.json, report.json, source fingerprint, archived harness,
review.md and six per-user JSONL files. No archive/dataset is regenerated.
Use ablations among validation trials to inspect the contribution of the cap and
decay; do not pick a new candidate because its test score looks better.

Both benchmark entry points reject outputs inside the immutable dataset root.
Configured K is reflected in metrics and review headings; default K=10.

## Executed results

Run `.tools/benchmarks/formula-v2-favi5k-001` completed the12trial search and final
evaluation on `favi-5k-v1`. All4959validation users and4943test users were warm.
Selected trial5 (zero-based): social_weight=1.5, decay=0, author_cap=2. Trial9
with social_weight=2.5 tied; the declared stable grid tie break chose trial5.
Selection validation nDCG=0.0059048686, Recall=0.0069837972. Full-stream replay of
validation exactly matched the prefix-only selection result.

| Test method | Recall@10 | nDCG@10 | Catalog coverage | Mean authors/top10 | Rank p95 ms |
|---|---:|---:|---:|---:|---:|
| Popularity |0.004257801 |0.003643638 |0.0065 |9.8628 |0.312 |
| Formula V1 |0.005224077 |0.004080534 |0.0360 |9.8651 |4.572 |
| Selected Formula V2 |0.006862654 |0.005726043 |0.0980 |5.6613 |3.758 |

V2 improved mean Recall by about31.4% and nDCG by40.3% relative to V1 on this
test split. Across users it reached196of2000catalog items vs72for V1, while
individual slates became more concentrated by author. The hard cap guarantees
no more than2items/author, but is less aggressive than V1's soft damping at
spreading the top10 among authors. It is therefore not an improvement on every
diversity measure. Maximum-author share averages.2for V2 vs.11297for V1.

Paired user bootstrap mean V2−V1 nDCG delta0.00164551 has percentile interval
[0.00044037,0.00278713]; Recall delta0.00163858 has interval
[0.00002212,0.00313117]. These are conditional on one synthetic graph and do not
establish superiority across datasets or real users. All2000posts existed before
validation; the synthetic response policy has no explicit item-age preference.
Consequently selecting no age decay is not a recommendation to remove freshness
from a real production feed.

Artifact hashes, frozen selection hash, population equality, unchanged Popularity
baseline and dataset integrity all passed. A second end-to-end control on the
small stored dataset with K=3 passed at `.tools/benchmarks/formula-v2-k3-control`.
79unit tests passed (67smoke +12adapter), including14ranking/tuning tests.
Application ranking and the raw dataset were not modified. EASE/LightGCN were not
executed in this experiment; V2 is a measured heuristic baseline for later
comparison, not proof of competitiveness against those models.

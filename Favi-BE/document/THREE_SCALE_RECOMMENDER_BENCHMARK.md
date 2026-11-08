# Recommender benchmark across three stored Favi datasets

The subsequent [fixed-scale multi-seed experiment](MULTISEED_RECOMMENDER_BENCHMARK.md)
holds size/config constant over three fresh 5k dataset seeds. It qualifies the
single-reference rankings here: EASE, LightGCN mean and PPR each lead nDCG on
one fresh dataset; no consistent collaborative winner has been established.

## Execution contract

Run Popularity, Formula V1, validation-selected Formula V2, EASE, exact bipartite
personalized PageRank and LightGCN on favi-2k-v1, favi-5k-v1 and favi-10k-v1.
Use the existing immutable datasets; no regeneration or production changes.
The 5k experiment is rerun with the same current harness as the other scales.

Protocol is `configs/recommenders/popularity.json`: K=10, train ticks <300,
validation [300,400), test [400,500). Test refits use events strictly before 400.
All-catalog candidates exclude own posts and prior likes; no sampled evaluation
negatives. Per-user candidate/target population fingerprints must agree across
methods within a dataset. Macro averages include users with eligible held-out
targets. Hyperparameters and LightGCN epoch counts are selected on validation
only, persisted before test, and their selection hashes checked after test.

Popularity/EASE/PPR/LightGCN use likes only. Formula V1/V2 additionally use prior
follows/comments/authorship; report their scores as a separate signal track.
The formula grid and model definitions remain those in
[FORMULA_V2_BENCHMARK.md](FORMULA_V2_BENCHMARK.md) and
[EXPANDED_RECOMMENDER_BENCHMARK.md](EXPANDED_RECOMMENDER_BENCHMARK.md).
The finite search budgets are unchanged across all three datasets. Each dataset
has independent validation selection; this measures a tuned solution at each
scale, not transfer of hyperparameters selected on the 5k corpus.

LightGCN uses three fixed training seeds (20261103, 20261104, 20261105), reporting
each result and mean/sample standard deviation. Training seeds are not independent
dataset replicates. The three dataset seeds and sizes both vary; differences
between their raw metrics do not isolate the effect of scale.

Runs execute sequentially, using the pinned Python environment and four numerical
CPU threads for the collaborative models. Full ranking kernel p95 is not HTTP
latency; numerical model-state bytes are not process/training peak RAM. Formula
and collaborative timings come from separate sequential stages. No universal
winner or human relevance claim follows from these synthetic datasets.

The desktop session experienced an extended wait during the 10k LightGCN tuning
stage. Search wall times can include interruptions and validation overhead;
they are not isolated training-speed measurements. Recorded refit costs and
kernel latencies remain descriptive measurements of this execution, not an SLA.

## Artifacts and reproduction

For each scale in `2k`, `5k`, `10k`, the new run paths are:

- `.tools/benchmarks/scales-20261103-<scale>-formula`
- `.tools/benchmarks/scales-20261103-<scale>-expanded`

Each completed run contains report.json, selection.json, per-user predictions,
source copies and a SHA256 manifest. Expanded runs also retain fitted NPZ arrays,
source-ID mappings, tuning trials and training histories. A report alone does not
mark completion: all manifest hashes and frozen selection hashes must pass.

```powershell
# Substitute 2k with 5k or 10k; always choose fresh output directories.
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_formula_benchmark.py --dataset .tools/datasets/favi-2k-v1 --output .tools/benchmarks/new-2k-formula
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_expanded_benchmark.py --dataset .tools/datasets/favi-2k-v1 --output .tools/benchmarks/new-2k-expanded --formula-run .tools/benchmarks/new-2k-formula
```

Results below are populated only from completed, verified run artifacts.

## Verified results

All six runs completed and their artifact/selection hashes passed. Dataset event
and SQLite fingerprints remain unchanged. All methods share candidate/target
population hashes within each dataset; Popularity exactly matches stored
readiness baselines for all metrics present in those historical reports. The 5k
readiness report predates author-diversity/max-author-share fields; these two
fields are checked against the later 5k formula/expanded runs instead. Every
5k quality metric exactly reproduces those later prior runs.

Machine-readable full precision results: [three-scale-20261103.json](benchmarks/three-scale-20261103.json).

| Dataset | Users | Posts | Likes | Validation users | Test users |
|---|---:|---:|---:|---:|---:|
| favi-2k-v1 | 2000 | 1000 | 53607 | 1984 | 1971 |
| favi-5k-v1 | 5000 | 2000 | 137198 | 4959 | 4943 |
| favi-10k-v1 | 10000 | 4000 | 277340 | 9907 | 9903 |

### favi-2k-v1

| Track | Method | Recall@10 | nDCG@10 | Coverage | Author diversity | Max author share |
|---|---|---:|---:|---:|---:|---:|
| Likes only | popularity | 0.01090976 | 0.00833550 | 0.0150 | 0.8094 | 0.2906 |
| Likes only | ease | 0.03932190 | 0.03230924 | 0.8430 | 0.5743 | 0.3201 |
| Likes only | ppr | 0.03975191 | 0.03325451 | 0.6840 | 0.5659 | 0.3174 |
| Likes only | LightGCN mean (3 seeds) | 0.03949685 | 0.03121142 | 0.8367 | 0.5542 | 0.3248 |
| Extra signals | favi_formula | 0.01331456 | 0.01089775 | 0.0390 | 0.9377 | 0.1518 |
| Extra signals | favi_formula_v2 | 0.01898453 | 0.01484481 | 0.2900 | 0.1367 | 0.8854 |

| LightGCN seed | Recall@10 | nDCG@10 | Fit seconds | State bytes | Rank p95 ms |
|---|---:|---:|---:|---:|---:|
| 20261103 | 0.03847224 | 0.02986931 | 5.041 | 1295772 | 0.0972 |
| 20261104 | 0.03858539 | 0.03089989 | 5.339 | 1295772 | 0.1028 |
| 20261105 | 0.04143292 | 0.03286507 | 5.252 | 1295772 | 0.0974 |

LightGCN sample std: Recall=0.00167764; nDCG=0.00152198.

Validation-selected parameters:

```json
{
  "formula_v2": {
    "social_weight": 2.5,
    "decay": 0,
    "author_cap": null
  },
  "ease_lambda": 1000.0,
  "ppr_alpha": 0.5,
  "lightgcn": {
    "dimensions": 64,
    "layers": 2,
    "learning_rate": 0.01,
    "regularization": 0.0001,
    "epochs": 50,
    "check_every": 10
  }
}
```

| Method | Fit seconds | State bytes | Rank p95 ms |
|---|---:|---:|---:|
| ease | 0.101 | 8527772 | 0.1664 |
| ppr | 0.087 | 8527772 | 0.1615 |
| popularity | 0.121 (Python tracemalloc) | not measured | 0.0959 |
| favi_formula | 0.304 (Python tracemalloc) | not measured | 2.2033 |
| favi_formula_v2 | 0.410 (Python tracemalloc) | not measured | 4.3369 |

Artifacts: `.tools/benchmarks/scales-20261103-2k-formula` and `.tools/benchmarks/scales-20261103-2k-expanded`.

### favi-5k-v1

| Track | Method | Recall@10 | nDCG@10 | Coverage | Author diversity | Max author share |
|---|---|---:|---:|---:|---:|---:|
| Likes only | popularity | 0.00425780 | 0.00364364 | 0.0065 | 0.9863 | 0.1132 |
| Likes only | ease | 0.02126195 | 0.01702500 | 0.9200 | 0.8797 | 0.1847 |
| Likes only | ppr | 0.01953604 | 0.01556382 | 0.5015 | 0.8631 | 0.1963 |
| Likes only | LightGCN mean (3 seeds) | 0.01973104 | 0.01587320 | 0.8692 | 0.8672 | 0.1912 |
| Extra signals | favi_formula | 0.00522408 | 0.00408053 | 0.0360 | 0.9865 | 0.1130 |
| Extra signals | favi_formula_v2 | 0.00686265 | 0.00572604 | 0.0980 | 0.5661 | 0.2000 |

| LightGCN seed | Recall@10 | nDCG@10 | Fit seconds | State bytes | Rank p95 ms |
|---|---:|---:|---:|---:|---:|
| 20261103 | 0.01989677 | 0.01567674 | 16.248 | 3136408 | 0.1832 |
| 20261104 | 0.01850929 | 0.01564313 | 15.597 | 3136408 | 0.1732 |
| 20261105 | 0.02078706 | 0.01629972 | 15.797 | 3136408 | 0.1740 |

LightGCN sample std: Recall=0.00114789; nDCG=0.00036977.

Validation-selected parameters:

```json
{
  "formula_v2": {
    "social_weight": 1.5,
    "decay": 0,
    "author_cap": 2
  },
  "ease_lambda": 1000.0,
  "ppr_alpha": 0.85,
  "lightgcn": {
    "dimensions": 64,
    "layers": 2,
    "learning_rate": 0.01,
    "regularization": 0.0001,
    "epochs": 60,
    "check_every": 10
  }
}
```

| Method | Fit seconds | State bytes | Rank p95 ms |
|---|---:|---:|---:|
| ease | 0.449 | 33344408 | 0.2569 |
| ppr | 0.390 | 33344408 | 0.2624 |
| popularity | 1.026 (Python tracemalloc) | not measured | 0.4565 |
| favi_formula | 1.240 (Python tracemalloc) | not measured | 8.9460 |
| favi_formula_v2 | 1.176 (Python tracemalloc) | not measured | 7.7216 |

Artifacts: `.tools/benchmarks/scales-20261103-5k-formula` and `.tools/benchmarks/scales-20261103-5k-expanded`.

### favi-10k-v1

| Track | Method | Recall@10 | nDCG@10 | Coverage | Author diversity | Max author share |
|---|---|---:|---:|---:|---:|---:|
| Likes only | popularity | 0.00270811 | 0.00185313 | 0.0030 | 1.0000 | 0.1000 |
| Likes only | ease | 0.00995515 | 0.00841507 | 0.9427 | 0.8879 | 0.1801 |
| Likes only | ppr | 0.00998596 | 0.00822945 | 0.6122 | 0.8775 | 0.1854 |
| Likes only | LightGCN mean (3 seeds) | 0.00943625 | 0.00783205 | 0.7775 | 0.8745 | 0.1887 |
| Extra signals | favi_formula | 0.00282891 | 0.00237504 | 0.0300 | 0.9964 | 0.1035 |
| Extra signals | favi_formula_v2 | 0.00469061 | 0.00392042 | 0.3145 | 0.1809 | 0.7978 |

| LightGCN seed | Recall@10 | nDCG@10 | Fit seconds | State bytes | Rank p95 ms |
|---|---:|---:|---:|---:|---:|
| 20261103 | 0.00941912 | 0.00741358 | 59.498 | 6296284 | 0.3528 |
| 20261104 | 0.00963727 | 0.00825196 | 59.769 | 6296284 | 0.3571 |
| 20261105 | 0.00925236 | 0.00783062 | 59.080 | 6296284 | 0.3589 |

LightGCN sample std: Recall=0.00019302; nDCG=0.00041919.

Validation-selected parameters:

```json
{
  "formula_v2": {
    "social_weight": 2.5,
    "decay": 0,
    "author_cap": null
  },
  "ease_lambda": 1000.0,
  "ppr_alpha": 0.85,
  "lightgcn": {
    "dimensions": 64,
    "layers": 2,
    "learning_rate": 0.001,
    "regularization": 0.0001,
    "epochs": 120,
    "check_every": 10
  }
}
```

| Method | Fit seconds | State bytes | Rank p95 ms |
|---|---:|---:|---:|
| ease | 1.978 | 130712284 | 0.8058 |
| ppr | 2.276 | 130712284 | 0.7819 |
| popularity | 0.667 (Python tracemalloc) | not measured | 0.7921 |
| favi_formula | 1.924 (Python tracemalloc) | not measured | 12.8311 |
| favi_formula_v2 | 1.846 (Python tracemalloc) | not measured | 10.3871 |

Artifacts: `.tools/benchmarks/scales-20261103-10k-formula` and `.tools/benchmarks/scales-20261103-10k-expanded`.

### Interpretation limits

Higher Recall/nDCG is better; coverage and author concentration describe different
properties and must be read alongside accuracy. Formula V2 maximizes validation
nDCG only, not diversity or fairness. In particular the 2k selected uncapped
formula has substantial author concentration despite improving accuracy.
Do not compare the separate signal tracks as a controlled algorithm-only test.
These are three synthetic datasets with different sizes and seeds; observed
rankings are conditional on this finite search, not statistical proof of a
universal winner or a causal scale effect. No production HTTP/API gate was run.

## Observed conclusions

Within the likes-only track, the highest observed test nDCG@10 is PPR on 2k,
and EASE on 5k and 10k. The 10k Recall order differs: PPR=0.00998596 and
EASE=0.00995515, a small point difference without a significance claim.
All three collaborative methods exceed Popularity on Recall and nDCG on all
three datasets. LightGCN does not consistently beat EASE/PPR under this finite
CPU/full-batch training budget. Its reported score is the mean of all three
declared seeds, never the best test seed.

Formula V2 improves both test accuracy metrics over V1 on every dataset, but
remains below the collaborative baselines here despite access to extra signals.
The selected uncapped formulas on 2k and 10k have substantial author
concentration: mean maximum-author shares 0.8854 and 0.7978. The 5k selected
hard cap has share 0.2. Optimizing validation nDCG alone therefore does not
guarantee acceptable author diversity. These findings are conditional on the
synthetic exposure/response policy and assumed one-hour ticks.

| Test nDCG@10 | 2k | 5k | 10k |
|---|---:|---:|---:|
| Popularity (likes only) | 0.00833550 | 0.00364364 | 0.00185313 |
| EASE (likes only) | 0.03230924 | **0.01702500** | **0.00841507** |
| PPR (likes only) | **0.03325451** | 0.01556382 | 0.00822945 |
| LightGCN (likes only, three-seed mean) | 0.03121142 | 0.01587320 | 0.00783205 |
| Formula V1 (extra signals) | 0.01089775 | 0.00408053 | 0.00237504 |
| Formula V2 (extra signals) | 0.01484481 | 0.00572604 | 0.00392042 |

Compare methods inside each column. Sizes, candidate counts and dataset seeds
change together; declining raw metrics across columns is not evidence that a
method becomes causally worse with scale. Hyperparameters were selected
independently per dataset; this does not measure cross-dataset transfer.

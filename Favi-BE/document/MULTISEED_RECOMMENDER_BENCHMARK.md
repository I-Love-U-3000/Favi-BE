# Fixed-scale recommender stability experiment

## Scope and frozen experiment

Action 1 only: generate three fresh 5,000-user datasets at seeds 20261106,
20261107 and 20261108, then evaluate Popularity, EASE, exact bipartite PPR and
LightGCN. Do not fetch external data or integrate production/API ranking in this
step. Stop after recording verified results for user review.

The experiment is declared in `tools/oasis-smoke/configs/recommenders/multiseed.json`.
All resolved generation configs must equal `configs/favi-5k.json` except seed:
5,000 users, 1,500 opening/2,000 maximum posts, 500 ticks, 8 topics, feed size 5,
exploration 0.5 and the same role/activity/quality policy. Actual naturally
generated interaction counts can differ. No failed seed is silently replaced,
and thresholds are not lowered after seeing outcomes. Each complete dataset must
pass native audit, event/state parity, export constraints and manifest validation;
its ZIP must pass member-by-member readback checks.

The original seed 20261101 is reported as a historical reference, not included
in the primary three-fresh-seed mean. Its early results informed the existing
model/search choices. The fresh datasets use one archived optimized generation
harness; verify matching harness hashes across all three. Historical 5k uses an
older harness; policy parity is covered by the documented sampler/native tests,
but it is not treated as a fresh out-of-sample dataset. No dataset is regenerated
to improve recommender scores.

## Evaluation and aggregation

Keep the existing K=10 protocol, global train/validation/test cutoffs 300/400,
full-catalog candidates, self/prior-like exclusions and binary likes-only input.
Use unchanged `expanded.json` search budget independently on each dataset.
Select parameters/epoch count with validation only; freeze before test. LightGCN
tests three fixed optimizer seeds 20261103/20261104/20261105 after refitting on
the prefix before 400. All candidate/target fingerprints must agree within each
dataset. Do not attach Formula results from another dataset.

For LightGCN, first average test metrics across its three optimizer seeds within
each dataset; retain all individual metrics and within-dataset sample standard
deviations. Then average each method's dataset-level scores equally across the
three fresh datasets. Report sample standard deviation, min/max, paired
dataset-level nDCG/Recall differences and win/tie/loss counts. Do not pool users
or count nine LightGCN optimizer runs as nine independent social datasets.
The reference dataset is displayed separately and never changes primary weights.

These are descriptive stability measurements over three simulated datasets,
not a statistical significance test, real-user validation or an estimate of a
causal scale effect. Different seeds still use the same exposure/response model.
Latency is descriptive desktop kernel timing, not HTTP latency or a production
SLA. Run benchmarks sequentially after all generators finish to avoid deliberate
CPU contention between our generation/training jobs.

## Artifacts and execution

Run commands from the solution root (`Favi-BE/Favi-BE`); declared experiment
paths are relative to that directory.

Keep generated data and benchmark/model artifacts under ignored `.tools` paths.
Store the experiment config, human-readable results and full precision aggregate
JSON in the project. Verify every benchmark manifest and frozen selection hash,
the raw dataset identity, shared evaluation populations and archived model source
versions before aggregation. The summary output must be fresh and outside inputs.

```powershell
./tools/oasis-smoke/run-dataset.ps1 -Config tools/oasis-smoke/configs/favi-5k.json -Seed 20261106 -Output .tools/datasets/favi-5k-seed-20261106-v1 -TimeoutSeconds 14400
# Repeat for seeds 20261107 and 20261108 into their declared new directories.
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_expanded_benchmark.py --dataset .tools/datasets/favi-5k-seed-20261106-v1 --output .tools/benchmarks/multiseed-5k-20261106-001
# Repeat sequentially for the other two seeds after all generation is complete.
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/summarize_multiseed_benchmark.py --output .tools/benchmarks/multiseed-5k-summary-001
```

The summary command verifies all raw bundles, archive receipts, benchmark hashes,
model-source equality and selection fingerprints before writing report.json,
review.md, frozen config/source copies and its own manifest. It refuses an
existing output. Six focused tests cover hierarchical averaging and optimizer
variation, missing optimizer seeds or inconsistent populations, duplicate dataset
seeds, paired signs, artifact mutation and manifest path escape. All six passed
before execution of the new benchmark batch. Model/evaluator implementations and
their search budgets have not been changed for this experiment.

Status: all three fresh datasets and benchmarks completed; raw/ZIP/artifact/selection checks passed. Full regression: 86 smoke/benchmark tests plus 12 adapter tests passed. No commit, push, external-data experiment or feed integration was performed in action 1.

## Verified results

Full precision result: [multiseed-5k-20261103.json](benchmarks/multiseed-5k-20261103.json).


Primary: three fresh dataset seeds; reference is excluded from means.
LightGCN optimizer runs are averaged within each dataset before aggregation.

| Method | Recall mean | Recall sample std | nDCG mean | nDCG sample std | nDCG min | nDCG max |
|---|---:|---:|---:|---:|---:|---:|
| popularity | 0.00499352 | 0.00033663 | 0.00387281 | 0.00033034 | 0.00349626 | 0.00411378 |
| ease | 0.01990531 | 0.00034511 | 0.01610449 | 0.00029317 | 0.01583580 | 0.01641717 |
| ppr | 0.02021861 | 0.00103427 | 0.01647590 | 0.00075833 | 0.01592845 | 0.01734148 |
| lightgcn | 0.02015265 | 0.00052197 | 0.01623641 | 0.00018069 | 0.01609930 | 0.01644116 |

| Dataset seed | Role | Likes | Test users | Method | Recall@10 | nDCG@10 | Within-dataset optimizer nDCG std |
|---|---|---:|---:|---|---:|---:|---:|
| 20261106 | primary | 136713 | 4965 | popularity | 0.00460705 | 0.00349626 | 0.00000000 |
| 20261106 | primary | 136713 | 4965 | ease | 0.02028574 | 0.01641717 | 0.00000000 |
| 20261106 | primary | 136713 | 4965 | ppr | 0.01948616 | 0.01592845 | 0.00000000 |
| 20261106 | primary | 136713 | 4965 | lightgcn | 0.02002485 | 0.01609930 | 0.00025316 |
| 20261107 | primary | 136752 | 4955 | popularity | 0.00522277 | 0.00411378 | 0.00000000 |
| 20261107 | primary | 136752 | 4955 | ease | 0.01981784 | 0.01606050 | 0.00000000 |
| 20261107 | primary | 136752 | 4955 | ppr | 0.01976792 | 0.01615779 | 0.00000000 |
| 20261107 | primary | 136752 | 4955 | lightgcn | 0.01970644 | 0.01616876 | 0.00048128 |
| 20261108 | primary | 137719 | 4953 | popularity | 0.00515074 | 0.00400841 | 0.00000000 |
| 20261108 | primary | 137719 | 4953 | ease | 0.01961236 | 0.01583580 | 0.00000000 |
| 20261108 | primary | 137719 | 4953 | ppr | 0.02140176 | 0.01734148 | 0.00000000 |
| 20261108 | primary | 137719 | 4953 | lightgcn | 0.02072664 | 0.01644116 | 0.00063258 |
| 20261101 | reference | 137198 | 4943 | popularity | 0.00425780 | 0.00364364 | 0.00000000 |
| 20261101 | reference | 137198 | 4943 | ease | 0.02126195 | 0.01702500 | 0.00000000 |
| 20261101 | reference | 137198 | 4943 | ppr | 0.01953604 | 0.01556382 | 0.00000000 |
| 20261101 | reference | 137198 | 4943 | lightgcn | 0.01973104 | 0.01587320 | 0.00036977 |

| Left minus right | nDCG mean delta | nDCG wins/ties/losses | Recall mean delta | Recall wins/ties/losses |
|---|---:|---|---:|---|
| popularity - ease | -0.01223168 | 0/0/3 | -0.01491179 | 0/0/3 |
| popularity - ppr | -0.01260309 | 0/0/3 | -0.01522509 | 0/0/3 |
| popularity - lightgcn | -0.01236359 | 0/0/3 | -0.01515912 | 0/0/3 |
| ease - ppr | -0.00037141 | 1/0/2 | -0.00031330 | 2/0/1 |
| ease - lightgcn | -0.00013192 | 1/0/2 | -0.00024734 | 2/0/1 |
| ppr - lightgcn | 0.00023950 | 1/0/2 | 0.00006597 | 2/0/1 |

## Validation-selected parameters

| Dataset seed | EASE lambda | PPR alpha | LightGCN layers | Learning rate | Epochs |
|---|---:|---:|---:|---:|---:|
| 20261106 | 100.0 | 0.85 | 2 | 0.001 | 270 |
| 20261107 | 1000.0 | 0.85 | 2 | 0.01 | 110 |
| 20261108 | 1000.0 | 0.5 | 1 | 0.01 | 70 |
| 20261101 | 1000.0 | 0.85 | 2 | 0.01 | 60 |

Full precision per-optimizer metrics, selections and artifact fingerprints: report.json.

## Limits

- Three fresh synthetic datasets, same behavior model, not real-user external validation
- Optimizer seeds averaged within each dataset before equal-weight dataset aggregation
- Historical reference excluded from primary means; no seed cherry-picking
- Descriptive sample std and paired signs, not statistical significance
- Full-batch CPU LightGCN and finite validation search, not exhaustive model comparison

## Review conclusions for action 1

All three collaborative baselines exceed Popularity on both Recall and nDCG in
all three fresh datasets. This is the consistent observation in this experiment.
Average nDCG is 0.01647590 for PPR, 0.01623641 for optimizer-averaged LightGCN and
0.01610449 for EASE, versus 0.00387281 for Popularity. The ranking among the
collaborative methods is not consistent across dataset seeds.

The highest observed nDCG per fresh dataset is EASE on 20261106, LightGCN mean
on 20261107 and PPR on 20261108. PPR has the highest mean nDCG but beats the
LightGCN dataset-level mean on only one of three datasets; its larger gain on
20261108 drives the average. EASE has higher Recall than PPR in two of three
datasets, while PPR has the higher average Recall. A mean score and a win count
answer different questions and should both be retained.

LightGCN's across-dataset nDCG standard deviation (0.00018069) is calculated
after averaging three optimizer runs within each dataset. Its within-dataset
optimizer nDCG deviations are 0.00025316, 0.00048128 and 0.00063258. The smaller
across-dataset deviation is not evidence that every individual optimizer run is
more stable. Baseline optimizer deviations shown as zero have one deterministic
fit, not an empirical variance estimate over multiple fits.

The prior claim that EASE led the original 5k test remains true for that stored
reference, but does not establish EASE as a universal 5k winner. This milestone
supports keeping multiple strong baselines and measuring uncertainty. Three
fresh synthetic datasets are insufficient to establish statistical superiority
or real-user effectiveness. No production algorithm is changed by these results.

Action 1 is complete and stopped for user review. External-data validation and
feed integration remain deferred until separately authorized.

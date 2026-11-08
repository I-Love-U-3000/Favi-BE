# Independent interaction-data benchmark: MovieLens 100K

## Scope, source and usage

Action 2 evaluates Popularity, EASE, PPR and LightGCN on independently collected
MovieLens 100K ratings, without OASIS, production writes or deployment of these
movie models. Official source and dataset terms:
[GroupLens README](https://files.grouplens.org/datasets/movielens/ml-100k/README).
The archive is downloaded from `https://files.grouplens.org/datasets/movielens/ml-100k.zip`.
GroupLens describes 100,000 ratings by 943 users on 1,682 movies, with UNIX-second
timestamps. This is explicit movie feedback, not Favi implicit social feedback.

Retain the original README and archive fingerprint locally. Use is confined to
this research benchmark. The dataset terms restrict redistribution and commercial
use; raw/derived data and model artifacts remain ignored local files, not Git
content or production assets. Project reports acknowledge GroupLens and cite:
F. Maxwell Harper and Joseph A. Konstan (2015), The MovieLens Datasets: History
and Context, ACM TiiS 5(4), Article 19, DOI 10.1145/2827872. No endorsement implied.

## Conversion and temporal protocol

Use only u.data ratings and u.item IDs; exclude demographics, titles and genres
from model features. Validate all raw counts, rating ranges, unique user/item
pairs and catalog references. Positives are ratings >=4; lower ratings are not
training positives, labels or verified negatives for this implicit task. Candidate
exclusions are prior positive items only, consistent across all methods. This
does not claim to reproduce a published MovieLens benchmark protocol.

Sort all raw ratings by timestamp, user ID and item ID. Assign every event at
the same timestamp the ordinal index of the first rating in its timestamp block.
Set total ticks=100000 and fixed cutoffs 60000/80000, derived from all ratings,
not positive-label quantiles. A timestamp block is never split between windows;
actual raw fractions may differ slightly from 60/20/20. Training uses only prior
positives, selection uses validation only, test refits use train+validation only.

Use a declared fixed catalog of all 1,682 IDs at tick=-1. This is a closed-catalog
evaluation assumption, not recovered historical movie availability. A sentinel
author -1 provides no real creator identity and cannot equal a user ID; author
diversity/concentration metrics must be N/A in reports and per-user outputs.
No fabricated social follows, comments, exposures or OASIS source DB is created.
The bundle has a dedicated verifier that rebuilds the conversion from the raw
archive; it must never masquerade as an audited native OASIS dataset.

Keep the existing K=10 all-catalog ranking and unchanged expanded.json finite
validation search, plus three fixed LightGCN optimizer seeds. Report eligible
warm/cold users, excluded positives/users, Recall/nDCG/coverage and training cost.
Optimizer seeds are not independent datasets. Comparison with synthetic results
concerns observed method ordering, not equal task difficulty or proof about Favi
users. Strong offline results do not establish online or causal improvement.

## Artifacts and commands

Run from the solution root using the pinned Python environment. Existing outputs
are refused. Source ZIP is read without filesystem extraction. Hash all declared
files and verify the deterministic event stream from its source archive.

```powershell
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/movielens_dataset.py --archive .tools/external-datasets/movielens-100k/ml-100k-official-001.zip --output .tools/datasets/movielens-100k-v1
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_expanded_benchmark.py --dataset .tools/datasets/movielens-100k-v1 --dataset-kind movielens100k --output .tools/benchmarks/movielens-100k-expanded-001
```

Native OASIS remains the default input kind. MovieLens support is explicit and
cannot attach historical Formula runs. Model math is shared; only source
verification and inapplicable author metrics differ. Results will be copied to
document/benchmarks after manifest/selection/population checks pass.

## Verified results

The source ZIP SHA256 is `50d2a982c66986937beb9ffb3aa76efe955bf3d5c6b761f4e3a7cd717c6a3229`. The bundle has 55,375 positive ratings; raw windows are 60,000/20,003/19,997 ratings and positive windows 34,006/10,066/11,303. All source/event conversions, artifact hashes, frozen selections and evaluation population fingerprints passed. The default native-input control also passed after adding the explicit external input kind. 89 Python smoke/benchmark regression tests passed.

There are 290 test users: 97 warm, 193 cold (no prefix positives). Cold collaborative scores are zero with deterministic item-ID ties, as in the existing evaluator; no test-time fallback was silently added.

| Method | All Recall@10 | All nDCG@10 | Warm nDCG@10 | Cold nDCG@10 |
|---|---:|---:|---:|---:|
| popularity | 0.07776591 | 0.26019822 | 0.11874932 | 0.33128911 |
| ease | 0.03533900 | 0.10745851 | 0.11402301 | 0.10415925 |
| ppr | 0.04294601 | 0.11109498 | 0.12489494 | 0.10415925 |
| lightgcn (three-seed mean) | 0.03911977 | 0.10925055 | 0.11938065 | 0.10415925 |

Popularity wins the declared all-user task. PPR has the highest warm nDCG in this bounded search, but its warm advantage is not a significance or online-uplift claim. Synthetic CF dominance therefore does not generalize to all external cohorts. Cold-start handling and cohort reporting are necessary; MovieLens explicit ratings differ from Favi behavior. This outcome was not used to revise splits, labels, candidates or select a better-looking test protocol.

Full precision metrics, optimizer seeds, parameters and provenance: [movielens-100k-20261103.json](benchmarks/movielens-100k-20261103.json). Raw ratings, predictions and model artifacts stay in ignored local storage.

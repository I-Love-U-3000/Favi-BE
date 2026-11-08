# Expanded likes-only benchmark: EASE, personalized PageRank and LightGCN

An explicit external-data input kind now reuses the same model math/search on
MovieLens, with dedicated source verification and N/A author metrics. Results
and cohort differences are in [EXTERNAL_RECOMMENDER_BENCHMARK.md](EXTERNAL_RECOMMENDER_BENCHMARK.md).
Historical runs retain their archived harness versions and are not rewritten.

The subsequent [three-scale benchmark](THREE_SCALE_RECOMMENDER_BENCHMARK.md)
reruns this same search on 2k, 5k and 10k datasets, with complete results and costs.
The original 5k quality results below were exactly reproduced.

## Controlled inputs and selection

Use immutable favi-5k-v1, the existing300/400global cutoffs, all available posts,
self/seen exclusions and identical per-user candidate/target hashes. Likes-only
track: Popularity/EASE/PPR/LightGCN. Existing Formula V1/V2 are separate solution
references because they additionally use follows/comments/authorship. No private
topics, simulator interests, exposure labels or final-state popularity enters
training. The immutable dataset is verified before running; outputs must be new
and outside it. Production ranking remains untouched.

Model definitions follow primary sources:
- EASE: https://arxiv.org/abs/1905.03375 (Algorithm1).
- LightGCN: https://arxiv.org/abs/2002.02126 (propagation and BPR objective).
- PPR definition and personalization/dangling semantics:
  https://networkx.org/documentation/stable/reference/algorithms/generated/networkx.algorithms.link_analysis.pagerank_alg.pagerank.html.

The finite search is versioned in configs/recommenders/expanded.json. EASE
lambda[1,10,100,1000]; PPR alpha[.5,.85,.95]; LightGCN layers[1,2] x learning
rate[.001,.01], dimensions64, regularization.0001. These are bounded local searches,
not exhaustive tuning or reproduction of published benchmark scores. Validation
macro nDCG@10 selects hyperparameters, with recall then stable trial index ties.
Persist selections before test. All test refits use prefix<400 and are evaluated
once per declared model/training seed; no test-based parameter/epoch/seed choice.

## Matrix and score contract

Build binary sparse X from prior likes only; reject duplicate pairs. Item metadata
comes from posts published before cutoff, including zero-like items. User nodes
are users with prior likes; a user without a training row receives zero scores
and deterministic source-ID ties. All methods use source post IDs for tie breaks.
No unseen user is assigned a learned embedding derived from future observations.

EASE solves P=(XᵀX+lambda*I)^-1, B_ij=-P_ij/P_jj and diag(B)=0, using float64
linear algebra. Query score is X_u B; negative weights/scores are not clipped.
The zero diagonal prevents copying observed items into themselves. It is still
necessary to apply the common seen/self eligibility filters after scoring.

PPR uses an undirected binary user–item graph, transition P_UI=D_u^-1X and
P_IU=D_i^-1Xᵀ, restart at the query user and continuation probability alpha per
hop. Eliminate user nodes analytically: item scores are
alpha*(1-alpha)*P_UI[u,:]*(I-alpha²*P_IU*P_UI)^-1. This Schur-complement solve is
the same personalized PageRank as a full bipartite random walk, not an arbitrary
item-similarity substitute. Zero-degree items get no propagated mass; cold users
have no item mass. Small-graph tests compare against full NetworkX PageRank.

LightGCN uses S=D^-1/2 A D^-1/2 on symmetric user–item adjacency; embedding
output is the uniform mean of layers0..L, without feature transforms/ReLU/dropout.
Train raw embeddings with Adam and full-batch BPR softplus(negative-positive),
plus .5*L2(raw user/positive/negative embeddings)/number_of_positive_edges.
One epoch is one full-positive-edge update with one newly sampled unobserved item
per edge. This full-batch schedule differs from the paper's minibatch workload;
report actual epoch counts, losses and bounded search, not paper-level optimality.
Negative samples exclude training likes only, never future validation/test likes.
Reject users with no available negative rather than looping forever.

CPU float32 Torch training uses deterministic algorithms,4threads and fixed seed.
Check validation after epoch1 and each10epochs, max300epochs, stop after5checks
without nDCG/recall improvement. Save the best validation checkpoint and epoch,
not just the last model. Select configuration on the declared training seed.
Test refits train from scratch for the selected epoch count on train+validation
using3predeclared seeds; report every result and mean/std without choosing the
best test seed. Save fitted numerical arrays in NPZ with source-ID mappings;
no pickle is needed. No CUDA or new package installation is required.

## Measurement and artifacts

NumPy/SciPy/BLAS and Torch CPU thread limits are recorded (4threads). Existing
locked environment versions are checked; dataset and protocol fingerprints,
search config, validation trials, training histories, selections, model weights,
per-user predictions, final results and file hashes are archived. Model state
bytes include fitted numerical arrays and sparse interaction matrix, not peak
training/process RAM. Fit time excludes shared prefix-to-CSR preparation, which
is measured separately. Per-query latency includes computing scores plus full
candidate filtering/ranking; scores for all users are not precomputed/cached.
Do not equate these kernel timings with production HTTP latency or historical
Formula timings measured in another run.

The common evaluator supplies Recall/nDCG/precision/hit rate, catalog coverage
and author concentration. Validate against prior Popularity results and saved
Formula population hashes before attaching references. All inputs remain the
same synthetic seed; training seeds measure optimizer variability, not different
societies or external validity. Test users are warm; this is not cold-start or
image-recommendation accuracy evaluation.

## Run

```powershell
./.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_expanded_benchmark.py --dataset .tools/datasets/favi-5k-v1 --output .tools/benchmarks/expanded-new-run --formula-run .tools/benchmarks/formula-v2-favi5k-001
```

Dense EASE/PPR solves are limited to5000items at both validation and test refit;
larger catalogs require another declared solver/budget. Fitted numerical state
counts parameter arrays plus CSR payload, excluding Python metadata, pair-index
arrays and optimizer/work buffers. Use fit_seconds on test rows for actual model
fitting. In the initial expanded run, inherited fit_ms_with_tracemalloc for
prefitted models is only evaluator handoff overhead; subsequent evaluator code
sets that unavailable field to null. It is not another training measurement.
PPR NPZ stores its item kernel; selection.json supplies alpha and hence the
alpha*(1-alpha) normalization when reconstructing absolute PageRank mass.

## Executed results

Run `.tools/benchmarks/expanded-favi5k-001` used the same4943test users/candidates
as the previous formula experiment. Selected only through validation: EASE
lambda1000; PPR alpha.85; LightGCN64dimensions,2layers, learning rate.01,
regularization.0001,60full-batch epochs. LightGCN selection validation nDCG
0.01681872; EASE0.01566295; PPR0.01665873. Test outcomes were not used to revise
these choices. The winning model family on validation and on test can differ.

| Track | Method | Test Recall@10 | Test nDCG@10 | Catalog coverage |
|---|---|---:|---:|---:|
| Likes only | Popularity |0.00425780 |0.00364364 |0.0065 |
| Likes only | EASE |0.02126195 |0.01702500 |0.9200 |
| Likes only | PPR |0.01953604 |0.01556382 |0.5015 |
| Likes only | LightGCN, mean of3training seeds |0.01973104 |0.01587320 |0.8692 |
| Extra-signal historical reference | Formula V1 |0.00522408 |0.00408053 |0.0360 |
| Extra-signal historical reference | Formula V2 |0.00686265 |0.00572604 |0.0980 |

LightGCN Recall across the3seeds is0.01989677,0.01850929,0.02078706; sample std
0.00114789. nDCG sample std0.00036977. No best test seed is selected. EASE has
the highest observed test mean in this bounded experiment; no universal ranking
or statistical significance across independent datasets is established.

Refit costs on CPU4threads: EASE.358seconds, PPR.369seconds, LightGCN
13.56–13.69seconds/seed. Numeric state+CSR:33344408bytes EASE/PPR and3136408bytes
LightGCN. Warm per-query p95 including scoring/filtering/ranking is.2482ms EASE,
.2223ms PPR and.1595–.1625ms LightGCN. Popularity's Python implementation measured
.3272ms; NumPy vectorization and implementation choices affect these timings.
Formula timing references are historical and not a controlled same-run speed test.
These costs are not total process RAM, production HTTP latency or scalability
claims for much larger catalogs.

89unit tests passed (77smoke +12adapter), including10collaborative-model tests.
EASE matches the closed-form reference; Schur PPR matches full NetworkX PageRank;
LightGCN propagation, deterministic training, negative sampling, checkpoint stop,
temporal boundary and saved numerical mappings were checked. Two complete small
control runs reproduced identical quality results. All artifact hashes, frozen
selection hashes, selected checkpoint validation replay, historical population
matches and unchanged Popularity baseline passed. Raw dataset revalidation still
matched all22declared file hashes. Stored weights, ID mappings, histories,
predictions and manifests are under the completed run directory.

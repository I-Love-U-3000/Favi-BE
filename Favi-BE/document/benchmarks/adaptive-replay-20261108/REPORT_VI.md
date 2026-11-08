# Fixed-selection replay: robustness và ablations

08/11/2026, Asia/Saigon. Secondary analysis theo adaptive-v1-20261108; source e5c23f0. Selection, model parameters, exact epochs và optimizer seeds giữ nguyên; không retune. Primary practical gate từ ranking gốc vẫn **FAIL** (main mean R−B1 +0.000334784 <0.001). Replay không thay kết luận đó.

Cả sáu datasets 5.000 users/2.000 posts/500 ticks giữ nguyên events, request400/targets[400,500), users/candidates/targets. Primary predictions được so từng ranked ID với evidence gốc cho đủ chín methods và optimizer seeds. Optimizer repeats gộp trong dataset trước; các bảng aggregate là mean đều ba dataset seeds, std là sample std của ba dataset means.

## main

| Mode | B1 nDCG@10 | R nDCG@10 | R−B1 mean | Dataset std delta | R−primary |
|---|---:|---:|---:|---:|---:|
| primary | 0.012925747 | 0.013260531 | 0.000334784 | 0.000661326 | 0.000000000 |
| informative | 0.012925747 | 0.013260531 | 0.000334784 | 0.000661326 | 0.000000000 |
| noisy | 0.011646620 | 0.010602228 | -0.001044392 | 0.000789403 | -0.002658303 |
| independent | 0.011613138 | 0.010113557 | -0.001499580 | 0.000620119 | -0.003146973 |
| no-topics | 0.011579665 | 0.010141730 | -0.001437935 | 0.000453957 | -0.003118801 |

### Đủ methods trên primary và ba sidecars

| Mode | Method | Mean nDCG@10 | Dataset std | Mean Recall@10 |
|---|---|---:|---:|---:|
| primary | B1 | 0.012925747 | 0.001030694 | 0.016370128 |
| primary | R | 0.013260531 | 0.000483263 | 0.016570033 |
| primary | formula-v2 | 0.005817985 | 0.000525305 | 0.007264029 |
| primary | ease | 0.012447980 | 0.000208119 | 0.015669368 |
| primary | ppr | 0.010293460 | 0.000668336 | 0.013018457 |
| primary | lightgcn | 0.013670212 | 0.000428759 | 0.017401512 |
| primary | R-no-routing | 0.012844365 | 0.000889931 | 0.016202047 |
| primary | popularity | 0.003498267 | 0.000272393 | 0.004811475 |
| primary | topic-only | 0.011028109 | 0.000297468 | 0.013394170 |
| primary | formula | 0.005501391 | 0.000427758 | 0.006822484 |
| informative | B1 | 0.012925747 | 0.001030694 | 0.016370128 |
| informative | R | 0.013260531 | 0.000483263 | 0.016570033 |
| informative | formula-v2 | 0.005817985 | 0.000525305 | 0.007264029 |
| informative | ease | 0.012447980 | 0.000208119 | 0.015669368 |
| informative | ppr | 0.010293460 | 0.000668336 | 0.013018457 |
| informative | lightgcn | 0.013670212 | 0.000428759 | 0.017401512 |
| informative | popularity | 0.003498267 | 0.000272393 | 0.004811475 |
| informative | topic-only | 0.011028109 | 0.000297468 | 0.013394170 |
| informative | formula | 0.005501391 | 0.000427758 | 0.006822484 |
| noisy | B1 | 0.011646620 | 0.000758076 | 0.014855980 |
| noisy | R | 0.010602228 | 0.000326369 | 0.013531519 |
| noisy | formula-v2 | 0.004538859 | 0.000292299 | 0.005749881 |
| noisy | ease | 0.011168854 | 0.000129035 | 0.014155220 |
| noisy | ppr | 0.009014334 | 0.000422796 | 0.011504309 |
| noisy | lightgcn | 0.012391085 | 0.000265001 | 0.015887364 |
| noisy | popularity | 0.003498267 | 0.000272393 | 0.004811475 |
| noisy | topic-only | 0.005471283 | 0.000042513 | 0.006630152 |
| noisy | formula | 0.004222264 | 0.000171046 | 0.005308336 |
| independent | B1 | 0.011613138 | 0.000702736 | 0.014864023 |
| independent | R | 0.010113557 | 0.000287704 | 0.012973096 |
| independent | formula-v2 | 0.004505376 | 0.000483693 | 0.005757923 |
| independent | ease | 0.011135371 | 0.000180643 | 0.014163262 |
| independent | ppr | 0.008980851 | 0.000316983 | 0.011512352 |
| independent | lightgcn | 0.012357603 | 0.000051350 | 0.015895407 |
| independent | popularity | 0.003498267 | 0.000272393 | 0.004811475 |
| independent | topic-only | 0.004122762 | 0.000139530 | 0.005279065 |
| independent | formula | 0.004188782 | 0.000361374 | 0.005316378 |
| no-topics | B1 | 0.011579665 | 0.000674311 | 0.014828507 |
| no-topics | R | 0.010141730 | 0.000489315 | 0.013053304 |

### Ablation tách routing khỏi joint selection

| Seed | R−B1 | R−R-no-routing | R-no-routing−B1 |
|---:|---:|---:|---:|
| 20261111 | 0.000481016 | 0.000481016 | 0.000000000 |
| 20261112 | -0.000387419 | -0.000143273 | -0.000244146 |
| 20261113 | 0.000910755 | 0.000910755 | 0.000000000 |
| Mean | 0.000334784 | 0.000416166 | -0.000081382 |

No-routing chỉ đổi threshold R thành1, giữ R model/config/epoch. No-topics giữ general cold fallback, không ép collaborative lên zero-positive users. Hiệu số trên cùng model cô lập threshold trong replay; không phải causal effect.

### Paired population, overlap và chuyển cohort

| Seed | Mode | Paired users | Changed topic users | Overlap/declared slots | Overlap rate |
|---:|---|---:|---:|---|---:|
| 20261111 | primary | 4921 | 0 | 6553/6553 | 1.000000 |
| 20261111 | informative | 4921 | 0 | 6553/6553 | 1.000000 |
| 20261111 | noisy | 4921 | 3281 | 1636/6553 | 0.249657 |
| 20261111 | independent | 4921 | 3111 | 1636/6553 | 0.249657 |
| 20261111 | no-topics | 4921 | 3281 | 0/0 | N/A |
| 20261112 | primary | 4935 | 0 | 6578/6578 | 1.000000 |
| 20261112 | informative | 4935 | 0 | 6578/6578 | 1.000000 |
| 20261112 | noisy | 4935 | 3290 | 1644/6578 | 0.249924 |
| 20261112 | independent | 4935 | 3137 | 1587/6578 | 0.241259 |
| 20261112 | no-topics | 4935 | 3290 | 0/0 | N/A |
| 20261113 | primary | 4919 | 0 | 6545/6545 | 1.000000 |
| 20261113 | informative | 4919 | 0 | 6545/6545 | 1.000000 |
| 20261113 | noisy | 4919 | 3279 | 1633/6545 | 0.249503 |
| 20261113 | independent | 4919 | 3101 | 1638/6545 | 0.250267 |
| 20261113 | no-topics | 4919 | 3279 | 0/0 | N/A |

Transitions đầy đủ theo từng seed:

- 20261111 primary: C0->C0=332; C1->C1=325; C2->C2=337; T1->T1=409; T2->T2=1006; W1->W1=1996; W2->W2=516.
- 20261111 informative: C0->C0=332; C1->C1=325; C2->C2=337; T1->T1=409; T2->T2=1006; W1->W1=1996; W2->W2=516.
- 20261111 noisy: C0->C0=332; C1->C1=325; C2->C2=337; T1->T1=409; T2->T2=1006; W1->W1=1996; W2->W2=516.
- 20261111 independent: C0->C0=332; C1->C1=325; C2->C2=337; T1->T1=409; T2->T2=1006; W1->W1=1996; W2->W2=516.
- 20261111 no-topics: C0->C0=332; C1->C0=325; C2->C0=337; T1->T1=409; T2->T2=1006; W1->W1=1996; W2->W2=516.
- 20261112 primary: C0->C0=335; C1->C1=327; C2->C2=336; T1->T1=359; T2->T2=1018; W1->W1=2041; W2->W2=519.
- 20261112 informative: C0->C0=335; C1->C1=327; C2->C2=336; T1->T1=359; T2->T2=1018; W1->W1=2041; W2->W2=519.
- 20261112 noisy: C0->C0=335; C1->C1=327; C2->C2=336; T1->T1=359; T2->T2=1018; W1->W1=2041; W2->W2=519.
- 20261112 independent: C0->C0=335; C1->C1=327; C2->C2=336; T1->T1=359; T2->T2=1018; W1->W1=2041; W2->W2=519.
- 20261112 no-topics: C0->C0=335; C1->C0=327; C2->C0=336; T1->T1=359; T2->T2=1018; W1->W1=2041; W2->W2=519.
- 20261113 primary: C0->C0=334; C1->C1=340; C2->C2=338; T1->T1=403; T2->T2=994; W1->W1=2014; W2->W2=496.
- 20261113 informative: C0->C0=334; C1->C1=340; C2->C2=338; T1->T1=403; T2->T2=994; W1->W1=2014; W2->W2=496.
- 20261113 noisy: C0->C0=334; C1->C1=340; C2->C2=338; T1->T1=403; T2->T2=994; W1->W1=2014; W2->W2=496.
- 20261113 independent: C0->C0=334; C1->C1=340; C2->C2=338; T1->T1=403; T2->T2=994; W1->W1=2014; W2->W2=496.
- 20261113 no-topics: C0->C0=334; C1->C0=340; C2->C0=338; T1->T1=403; T2->T2=994; W1->W1=2014; W2->W2=496.

### Cohort: cả phân nhóm gốc và phân nhóm hiện tại

| Mode | Partition | Cohort | Dataset count đủ ≥30 | Mean R−B1 | R−primary trên primary cohort |
|---|---|---|---:|---:|---:|
| primary | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | C1 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | C2 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | T1 | 3 | 0.001508894 | 0.000000000 |
| primary | primary_cohorts | T2 | 3 | 0.001114060 | 0.000000000 |
| primary | primary_cohorts | W1 | 3 | -0.000003689 | 0.000000000 |
| primary | primary_cohorts | W2 | 3 | -0.000198901 | 0.000000000 |
| primary | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | C1 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | C2 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | T1 | 3 | 0.001508894 | N/A |
| primary | adaptive_cohorts | T2 | 3 | 0.001114060 | N/A |
| primary | adaptive_cohorts | W1 | 3 | -0.000003689 | N/A |
| primary | adaptive_cohorts | W2 | 3 | -0.000198901 | N/A |
| informative | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| informative | primary_cohorts | C1 | 3 | 0.000000000 | 0.000000000 |
| informative | primary_cohorts | C2 | 3 | 0.000000000 | 0.000000000 |
| informative | primary_cohorts | T1 | 3 | 0.001508894 | 0.000000000 |
| informative | primary_cohorts | T2 | 3 | 0.001114060 | 0.000000000 |
| informative | primary_cohorts | W1 | 3 | -0.000003689 | 0.000000000 |
| informative | primary_cohorts | W2 | 3 | -0.000198901 | 0.000000000 |
| informative | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | C1 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | C2 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | T1 | 3 | 0.001508894 | N/A |
| informative | adaptive_cohorts | T2 | 3 | 0.001114060 | N/A |
| informative | adaptive_cohorts | W1 | 3 | -0.000003689 | N/A |
| informative | adaptive_cohorts | W2 | 3 | -0.000198901 | N/A |
| noisy | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| noisy | primary_cohorts | C1 | 3 | 0.000000000 | -0.016253952 |
| noisy | primary_cohorts | C2 | 3 | 0.000000000 | -0.002801568 |
| noisy | primary_cohorts | T1 | 3 | -0.002484380 | -0.003993274 |
| noisy | primary_cohorts | T2 | 3 | -0.004108739 | -0.005222799 |
| noisy | primary_cohorts | W1 | 3 | -0.000003689 | 0.000000000 |
| noisy | primary_cohorts | W2 | 3 | -0.000198901 | 0.000000000 |
| noisy | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | C1 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | C2 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | T1 | 3 | -0.002484380 | N/A |
| noisy | adaptive_cohorts | T2 | 3 | -0.004108739 | N/A |
| noisy | adaptive_cohorts | W1 | 3 | -0.000003689 | N/A |
| noisy | adaptive_cohorts | W2 | 3 | -0.000198901 | N/A |
| independent | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| independent | primary_cohorts | C1 | 3 | 0.000000000 | -0.012262469 |
| independent | primary_cohorts | C2 | 3 | 0.000000000 | -0.007160330 |
| independent | primary_cohorts | T1 | 3 | -0.003073405 | -0.004582299 |
| independent | primary_cohorts | T2 | 3 | -0.006087889 | -0.007201949 |
| independent | primary_cohorts | W1 | 3 | -0.000003689 | 0.000000000 |
| independent | primary_cohorts | W2 | 3 | -0.000198901 | 0.000000000 |
| independent | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | C1 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | C2 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | T1 | 3 | -0.003073405 | N/A |
| independent | adaptive_cohorts | T2 | 3 | -0.006087889 | N/A |
| independent | adaptive_cohorts | W1 | 3 | -0.000003689 | N/A |
| independent | adaptive_cohorts | W2 | 3 | -0.000198901 | N/A |
| no-topics | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| no-topics | primary_cohorts | C1 | 3 | 0.000000000 | -0.012767105 |
| no-topics | primary_cohorts | C2 | 3 | 0.000000000 | -0.007204367 |
| no-topics | primary_cohorts | T1 | 3 | -0.004579536 | -0.006088430 |
| no-topics | primary_cohorts | T2 | 3 | -0.005224409 | -0.006338469 |
| no-topics | primary_cohorts | W1 | 3 | -0.000003689 | 0.000000000 |
| no-topics | primary_cohorts | W2 | 3 | -0.000198901 | 0.000000000 |
| no-topics | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| no-topics | adaptive_cohorts | T1 | 3 | -0.004579536 | N/A |
| no-topics | adaptive_cohorts | T2 | 3 | -0.005224409 | N/A |
| no-topics | adaptive_cohorts | W1 | 3 | -0.000003689 | N/A |
| no-topics | adaptive_cohorts | W2 | 3 | -0.000198901 | N/A |

Không so current-cohort means với primary-cohort means như cùng một nhóm người. JSON giữ denominators và optimizer std của từng method/mode/dataset; cohort không có target là N/A.

## sparse

| Mode | B1 nDCG@10 | R nDCG@10 | R−B1 mean | Dataset std delta | R−primary |
|---|---:|---:|---:|---:|---:|
| primary | 0.010601171 | 0.010601171 | 0.000000000 | 0.000000000 | 0.000000000 |
| informative | 0.011487234 | 0.011487234 | 0.000000000 | 0.000000000 | 0.000886063 |
| noisy | 0.010541423 | 0.010541423 | 0.000000000 | 0.000000000 | -0.000059748 |
| independent | 0.010303108 | 0.010303108 | 0.000000000 | 0.000000000 | -0.000298063 |
| no-topics | 0.010252875 | 0.010252875 | 0.000000000 | 0.000000000 | -0.000348296 |

### Đủ methods trên primary và ba sidecars

| Mode | Method | Mean nDCG@10 | Dataset std | Mean Recall@10 |
|---|---|---:|---:|---:|
| primary | B1 | 0.010601171 | 0.000500912 | 0.013610976 |
| primary | R | 0.010601171 | 0.000500912 | 0.013610976 |
| primary | formula-v2 | 0.004711142 | 0.000615215 | 0.005862408 |
| primary | ease | 0.011231291 | 0.000506981 | 0.014017699 |
| primary | ppr | 0.007949836 | 0.000405986 | 0.010086212 |
| primary | lightgcn | 0.010676852 | 0.000537614 | 0.013818803 |
| primary | R-no-routing | 0.010601171 | 0.000500912 | 0.013610976 |
| primary | popularity | 0.003616857 | 0.000536820 | 0.004908575 |
| primary | topic-only | 0.004555654 | 0.000342924 | 0.006438055 |
| primary | formula | 0.004244103 | 0.000747093 | 0.005575130 |
| informative | B1 | 0.011487234 | 0.000639114 | 0.014839083 |
| informative | R | 0.011487234 | 0.000639114 | 0.014839083 |
| informative | formula-v2 | 0.005597204 | 0.000752710 | 0.007090516 |
| informative | ease | 0.012117354 | 0.000484672 | 0.015245806 |
| informative | ppr | 0.008835898 | 0.000299321 | 0.011314319 |
| informative | lightgcn | 0.011562915 | 0.000665270 | 0.015046911 |
| informative | popularity | 0.003616857 | 0.000536820 | 0.004908575 |
| informative | topic-only | 0.010734030 | 0.000292375 | 0.013721244 |
| informative | formula | 0.005130166 | 0.000879914 | 0.006803238 |
| noisy | B1 | 0.010541423 | 0.000360167 | 0.013524500 |
| noisy | R | 0.010541423 | 0.000360167 | 0.013524500 |
| noisy | formula-v2 | 0.004651394 | 0.000173455 | 0.005775933 |
| noisy | ease | 0.011171543 | 0.000630232 | 0.013931224 |
| noisy | ppr | 0.007890088 | 0.000883990 | 0.009999737 |
| noisy | lightgcn | 0.010617104 | 0.000487730 | 0.013732328 |
| noisy | popularity | 0.003616857 | 0.000536820 | 0.004908575 |
| noisy | topic-only | 0.004847506 | 0.000495174 | 0.006084627 |
| noisy | formula | 0.004184355 | 0.000272653 | 0.005488655 |
| independent | B1 | 0.010303108 | 0.000222497 | 0.013296461 |
| independent | R | 0.010303108 | 0.000222497 | 0.013296461 |
| independent | formula-v2 | 0.004413079 | 0.000337747 | 0.005547893 |
| independent | ease | 0.010933228 | 0.000664229 | 0.013703184 |
| independent | ppr | 0.007651773 | 0.000676381 | 0.009771697 |
| independent | lightgcn | 0.010378789 | 0.000308738 | 0.013504288 |
| independent | popularity | 0.003616857 | 0.000536820 | 0.004908575 |
| independent | topic-only | 0.004101736 | 0.000241964 | 0.005591335 |
| independent | formula | 0.003946040 | 0.000484672 | 0.005260615 |
| no-topics | B1 | 0.010252875 | 0.000403862 | 0.013204814 |
| no-topics | R | 0.010252875 | 0.000403862 | 0.013204814 |

### Ablation tách routing khỏi joint selection

| Seed | R−B1 | R−R-no-routing | R-no-routing−B1 |
|---:|---:|---:|---:|
| 20261111 | 0.000000000 | 0.000000000 | 0.000000000 |
| 20261112 | 0.000000000 | 0.000000000 | 0.000000000 |
| 20261113 | 0.000000000 | 0.000000000 | 0.000000000 |
| Mean | 0.000000000 | 0.000000000 | 0.000000000 |

No-routing chỉ đổi threshold R thành1, giữ R model/config/epoch. No-topics giữ general cold fallback, không ép collaborative lên zero-positive users. Hiệu số trên cùng model cô lập threshold trong replay; không phải causal effect.

### Paired population, overlap và chuyển cohort

| Seed | Mode | Paired users | Changed topic users | Overlap/declared slots | Overlap rate |
|---:|---|---:|---:|---|---:|
| 20261111 | primary | 4923 | 0 | 3283/3283 | 1.000000 |
| 20261111 | informative | 4923 | 3107 | 659/6565 | 0.100381 |
| 20261111 | noisy | 4923 | 3283 | 169/6565 | 0.025743 |
| 20261111 | independent | 4923 | 3147 | 644/6565 | 0.098096 |
| 20261111 | no-topics | 4923 | 3283 | 0/0 | N/A |
| 20261112 | primary | 4931 | 0 | 3282/3282 | 1.000000 |
| 20261112 | informative | 4931 | 3106 | 679/6552 | 0.103632 |
| 20261112 | noisy | 4931 | 3282 | 166/6552 | 0.025336 |
| 20261112 | independent | 4931 | 3117 | 641/6552 | 0.097833 |
| 20261112 | no-topics | 4931 | 3282 | 0/0 | N/A |
| 20261113 | primary | 4925 | 0 | 3286/3286 | 1.000000 |
| 20261113 | informative | 4925 | 3117 | 649/6564 | 0.098873 |
| 20261113 | noisy | 4925 | 3286 | 164/6564 | 0.024985 |
| 20261113 | independent | 4925 | 3114 | 671/6564 | 0.102224 |
| 20261113 | no-topics | 4925 | 3286 | 0/0 | N/A |

Transitions đầy đủ theo từng seed:

- 20261111 primary: C0->C0=356; C3->C3=704; T1->T1=351; T2->T2=979; W1->W1=2028; W2->W2=505.
- 20261111 informative: C0->C0=356; C3->C1=312; C3->C2=355; C3->C3=37; T1->T1=351; T2->T2=979; W1->W1=2028; W2->W2=505.
- 20261111 noisy: C0->C0=356; C3->C1=349; C3->C2=355; T1->T1=351; T2->T2=979; W1->W1=2028; W2->W2=505.
- 20261111 independent: C0->C0=356; C3->C1=321; C3->C2=355; C3->C3=28; T1->T1=351; T2->T2=979; W1->W1=2028; W2->W2=505.
- 20261111 no-topics: C0->C0=356; C3->C0=704; T1->T1=351; T2->T2=979; W1->W1=2028; W2->W2=505.
- 20261112 primary: C0->C0=348; C3->C3=675; T1->T1=374; T2->T2=981; W1->W1=2068; W2->W2=485.
- 20261112 informative: C0->C0=348; C3->C1=297; C3->C2=334; C3->C3=44; T1->T1=374; T2->T2=981; W1->W1=2068; W2->W2=485.
- 20261112 noisy: C0->C0=348; C3->C1=341; C3->C2=334; T1->T1=374; T2->T2=981; W1->W1=2068; W2->W2=485.
- 20261112 independent: C0->C0=348; C3->C1=312; C3->C2=334; C3->C3=29; T1->T1=374; T2->T2=981; W1->W1=2068; W2->W2=485.
- 20261112 no-topics: C0->C0=348; C3->C0=675; T1->T1=374; T2->T2=981; W1->W1=2068; W2->W2=485.
- 20261113 primary: C0->C0=346; C3->C3=671; T1->T1=367; T2->T2=985; W1->W1=2059; W2->W2=497.
- 20261113 informative: C0->C0=346; C3->C1=299; C3->C2=338; C3->C3=34; T1->T1=367; T2->T2=985; W1->W1=2059; W2->W2=497.
- 20261113 noisy: C0->C0=346; C3->C1=333; C3->C2=338; T1->T1=367; T2->T2=985; W1->W1=2059; W2->W2=497.
- 20261113 independent: C0->C0=346; C3->C1=300; C3->C2=338; C3->C3=33; T1->T1=367; T2->T2=985; W1->W1=2059; W2->W2=497.
- 20261113 no-topics: C0->C0=346; C3->C0=671; T1->T1=367; T2->T2=985; W1->W1=2059; W2->W2=497.

### Cohort: cả phân nhóm gốc và phân nhóm hiện tại

| Mode | Partition | Cohort | Dataset count đủ ≥30 | Mean R−B1 | R−primary trên primary cohort |
|---|---|---|---:|---:|---:|
| primary | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | C3 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | T1 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | T2 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | W1 | 3 | 0.000000000 | 0.000000000 |
| primary | primary_cohorts | W2 | 3 | 0.000000000 | 0.000000000 |
| primary | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | C3 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | T1 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | T2 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | W1 | 3 | 0.000000000 | N/A |
| primary | adaptive_cohorts | W2 | 3 | 0.000000000 | N/A |
| informative | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| informative | primary_cohorts | C3 | 3 | 0.000000000 | 0.006391144 |
| informative | primary_cohorts | T1 | 3 | 0.000000000 | 0.000000000 |
| informative | primary_cohorts | T2 | 3 | 0.000000000 | 0.000000000 |
| informative | primary_cohorts | W1 | 3 | 0.000000000 | 0.000000000 |
| informative | primary_cohorts | W2 | 3 | 0.000000000 | 0.000000000 |
| informative | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | C1 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | C2 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | C3 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | T1 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | T2 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | W1 | 3 | 0.000000000 | N/A |
| informative | adaptive_cohorts | W2 | 3 | 0.000000000 | N/A |
| noisy | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| noisy | primary_cohorts | C3 | 3 | 0.000000000 | -0.000460242 |
| noisy | primary_cohorts | T1 | 3 | 0.000000000 | 0.000000000 |
| noisy | primary_cohorts | T2 | 3 | 0.000000000 | 0.000000000 |
| noisy | primary_cohorts | W1 | 3 | 0.000000000 | 0.000000000 |
| noisy | primary_cohorts | W2 | 3 | 0.000000000 | 0.000000000 |
| noisy | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | C1 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | C2 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | T1 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | T2 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | W1 | 3 | 0.000000000 | N/A |
| noisy | adaptive_cohorts | W2 | 3 | 0.000000000 | N/A |
| independent | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| independent | primary_cohorts | C3 | 3 | 0.000000000 | -0.002150072 |
| independent | primary_cohorts | T1 | 3 | 0.000000000 | 0.000000000 |
| independent | primary_cohorts | T2 | 3 | 0.000000000 | 0.000000000 |
| independent | primary_cohorts | W1 | 3 | 0.000000000 | 0.000000000 |
| independent | primary_cohorts | W2 | 3 | 0.000000000 | 0.000000000 |
| independent | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | C1 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | C2 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | C3 | 1 | 0.000000000 | N/A |
| independent | adaptive_cohorts | T1 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | T2 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | W1 | 3 | 0.000000000 | N/A |
| independent | adaptive_cohorts | W2 | 3 | 0.000000000 | N/A |
| no-topics | primary_cohorts | C0 | 3 | 0.000000000 | 0.000000000 |
| no-topics | primary_cohorts | C3 | 3 | 0.000000000 | -0.002534776 |
| no-topics | primary_cohorts | T1 | 3 | 0.000000000 | 0.000000000 |
| no-topics | primary_cohorts | T2 | 3 | 0.000000000 | 0.000000000 |
| no-topics | primary_cohorts | W1 | 3 | 0.000000000 | 0.000000000 |
| no-topics | primary_cohorts | W2 | 3 | 0.000000000 | 0.000000000 |
| no-topics | adaptive_cohorts | C0 | 3 | 0.000000000 | N/A |
| no-topics | adaptive_cohorts | T1 | 3 | 0.000000000 | N/A |
| no-topics | adaptive_cohorts | T2 | 3 | 0.000000000 | N/A |
| no-topics | adaptive_cohorts | W1 | 3 | 0.000000000 | N/A |
| no-topics | adaptive_cohorts | W2 | 3 | 0.000000000 | N/A |

Không so current-cohort means với primary-cohort means như cùng một nhóm người. JSON giữ denominators và optimizer std của từng method/mode/dataset; cohort không có target là N/A.

## Diễn giải kết quả secondary

Main: R−B1 đổi từ +0.000334784 với primary/informative thành −0.001044392 khi noisy, −0.001499580 khi independent và −0.001437935 khi bỏ topics. Cả ba dataset deltas đều âm trong ba mode sau; đây là dấu hiệu router đã chọn phụ thuộc chất lượng topics trong thiết kế synthetic này. Không chọn lại threshold theo sidecar để cứu kết quả.

Ablation main: giữ cấu hình R, đổi threshold từ1 sang threshold đã chọn cho mean +0.000416166; phần khác biệt R-no-routing−B1 là −0.000081382. Thành phần threshold không thắng mọi seed (min −0.000143273); tổng joint R vẫn không đạt practical gate. Các hiệu số này là mô tả replay, không causal effect hoặc significance.

Sparse: R=B1 trong mọi mode vì selection gốc trùng nhau ở threshold1. Informative tăng cả hai so primary +0.000886063 nhưng sidecar là pre-scarcity và chuyển cohort; không diễn giải thành lợi ích routing hoặc cải thiện trên cùng current cohort C3.

## Provenance và kiểm chứng

Cả sáu preflight kiểm ZIP receipt SHA256, mọi dataset member, original ranking manifests và tracked batch/report/selection/source hashes trước fit. Source/config/lock/CLI inventory và frozen-source.zip lưu trong batch. Một preflight attempt lỗi trước model do script yêu cầu thêm tracked protocol.json không tồn tại; giữ nguyên ở adaptive-replay-20261108-preflight-failed-01. Sửa kiểm chứng đường dẫn, không sửa dữ liệu/model/selection; batch mới dừng ngay nếu lỗi.

Independent audit tính lại mọi per-user nDCG/Recall từ ranked IDs, cohort means theo hai partition, common original users/candidates/targets, primary exact prediction parity, overlap/transitions, frozen choices và optimizer hierarchy. Không fit lại để kiểm số học. Xem audit.json, summary.json, batch.json, source-inventory.json và archive.json cạnh báo cáo. Raw/logs/ZIP ở solution .tools, không đưa vào Git.

## Giới hạn và việc còn lại

Sparse primary dùng music-only scarcity, trong khi informative/noisy/independent sidecars là metadata trước scarcity. Vì vậy sparse informative khác primary và có thể chuyển C3 sang C1/C2. Giữ mọi paired users, không sửa sidecars hay chọn subset để bảo toàn cohort.

Đây là synthetic metadata replay với event log cố định, không đo thay đổi hành vi/exposure hoặc causal engagement. Ba dataset seeds chỉ mô tả ổn định, không chứng minh significance/novelty. Primary practical gate thất bại vẫn được giữ. Refit seconds không phải full training cost, peak RAM hoặc HTTP latency.

Full cost và HTTP lifecycle/fallback/privacy/resource gates còn phải làm. Endpoint thử nghiệm hiện chỉ EASE/PPR/threshold1; không gọi HTTP evidence cũ là serving R/LightGCN. Giới hạn sample ≤1.000 users/16MiB/300 candidates vẫn giữ, không gọi là full5k deployment. Không merge/deploy tự ý.

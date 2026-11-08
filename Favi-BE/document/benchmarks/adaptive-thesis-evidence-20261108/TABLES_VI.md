# Bảng số liệu sinh từ JSON đã khóa

Sinh bằng build-evidence.ps1; -- không chạy model. nDCG@10 trung bình optimizer trong dataset trước, rồi trung bình đều ba dataset. Số hiển thị chín chữ số thập phân; JSON nguồn giữ độ chính xác gốc.

## Chất lượng toàn tập có target

| Scenario | Method | Mean nDCG@10 | Dataset sample std | Mean Recall@10 |
|---|---|---:|---:|---:|
| main | popularity | 0.003498267 | 0.000272393 | 0.004811475 |
| main | topic-only | 0.011028109 | 0.000297468 | 0.013394170 |
| main | formula | 0.005501391 | 0.000427758 | 0.006822484 |
| main | formula-v2 | 0.005817985 | 0.000525305 | 0.007264029 |
| main | ease | 0.012447980 | 0.000208119 | 0.015669368 |
| main | ppr | 0.010293460 | 0.000668336 | 0.013018457 |
| main | lightgcn | 0.013670212 | 0.000428759 | 0.017401512 |
| main | B1 | 0.012925747 | 0.001030694 | 0.016370128 |
| main | R | 0.013260531 | 0.000483263 | 0.016570033 |
| sparse | popularity | 0.003616857 | 0.000536820 | 0.004908575 |
| sparse | topic-only | 0.004555654 | 0.000342924 | 0.006438055 |
| sparse | formula | 0.004244103 | 0.000747093 | 0.005575130 |
| sparse | formula-v2 | 0.004711142 | 0.000615215 | 0.005862408 |
| sparse | ease | 0.011231291 | 0.000506981 | 0.014017699 |
| sparse | ppr | 0.007949836 | 0.000405986 | 0.010086212 |
| sparse | lightgcn | 0.010676852 | 0.000537614 | 0.013818803 |
| sparse | B1 | 0.010601171 | 0.000500912 | 0.013610976 |
| sparse | R | 0.010601171 | 0.000500912 | 0.013610976 |

## Chất lượng theo lịch sử: main

C0/C1/C2: 0 positive, lần lượt 0/1/2–10 topics; T1: 1 positive; T2: 2–4; W1: 5–19; W2: từ 20. Đây là các nhóm người khác nhau tại cutoff400, không phải đường tiến bộ theo thời gian của cùng người.

| Cohort | Target users seeds11/12/13 | B1 mean | R mean | R−B1 | Popularity mean | Topic-only mean |
|---|---|---:|---:|---:|---:|---:|
| C0 | 332/335/334 | 0.002986432 | 0.002986432 | 0.000000000 | 0.003770260 | 0.002986432 |
| C1 | 325/327/340 | 0.018381056 | 0.018381056 | 0.000000000 | 0.002508096 | 0.018381056 |
| C2 | 337/336/338 | 0.011714377 | 0.011714377 | 0.000000000 | 0.003274679 | 0.011714377 |
| T1 | 409/359/403 | 0.007854566 | 0.009363460 | 0.001508894 | 0.003535071 | 0.009363460 |
| T2 | 1006/1018/994 | 0.009711464 | 0.010825524 | 0.001114060 | 0.003539136 | 0.010825524 |
| W1 | 1996/2041/2014 | 0.014301783 | 0.014298094 | -0.000003689 | 0.003570005 | 0.010621816 |
| W2 | 516/519/496 | 0.021641749 | 0.021442849 | -0.000198901 | 0.003726737 | 0.014401363 |

## Robustness main: cùng paired users, selection giữ nguyên

| Mode | B1 mean | R mean | R−B1 |
|---|---:|---:|---:|
| primary | 0.012925747 | 0.013260531 | 0.000334784 |
| informative | 0.012925747 | 0.013260531 | 0.000334784 |
| noisy | 0.011646620 | 0.010602228 | -0.001044392 |
| independent | 0.011613138 | 0.010113557 | -0.001499580 |
| no-topics | 0.011579665 | 0.010141730 | -0.001437935 |

## Nguồn và SHA256 của đúng bytes đầu vào

Hash xác nhận file đã dùng để tổng hợp, không xác nhận telemetry chưa được đo. Raw archive nằm ở .tools, không được nhúng trong gói này.

| File tương đối từ document/benchmarks | SHA256 |
|---|---|
| adaptive-confirmatory-ranking-20261108/summary.json | de13b34af274a6e8dcd9da89e6c528c76c28bd11b8d73706d25e132642544b93 |
| adaptive-replay-20261108/summary.json | 6f58ff0361f1d62a821630c0a81fc87aa5420c8c756093cb7ae7c9a06ef55bef |
| adaptive-cost-audit-20261108/summary.json | 61823241d9b1ff658566c424380ff8256b2aebf76f5edcf4fcf5fd931e290cdf |
| adaptive-http-20261108/summary.json | d40306ea9fcc21af0761ad7449ee1c5fa1a5f09af404f4d2f05fcf0d7eb322ad |
| adaptive-serving-export-20261108/provenance.json | 5c09ab1f1e3401e6b636ae00f8d15f3ac743da459c0b21541193fa1227af3b56 |

# Confirmatory adaptive ranking: primary practical gate FAIL

08/11/2026, Asia/Saigon. Protocol `adaptive-v1-20261108`, source commit `e72dcd3`.
Cả sáu qualified datasets chạy tuần tự thành công, không failed run, đổi seed, hạ gate,
đổi source/config hay retune theo test. Runner readiness 111 tests tại `2ea65f8` được giữ
nguyên; milestone này kiểm thực tế sáu datasets và kiểm toán toàn bộ outputs, không rerun parity/pilot.

Kết luận chính: R−B1 main mean nDCG@10 = **+0.000334784**, thấp hơn practical gain
0.001 đã khóa. Hai seed dương/một seed âm; không cohort main đủ mẫu nào có mean loss
vượt 0.002. Vì gain tổng thể không đủ, **bác bỏ claim lợi ích thực dụng của joint router R
so với B1 trong thiết kế frozen này**. Không đổi ngưỡng sau test để cứu claim.

## Thiết kế và selection

Train <300; validation request 300/targets[300,400); refit <400;
test request 400/targets[400,500). Features/arrival/topics chỉ thấy trước request.
Chung users/candidates/targets trong mỗi phase; eligible loại own/prior-positive/future items.
Selection chỉ nhận prefix <400, tối ưu validation macro nDCG@10; tie theo frozen protocol.
`selection.json` được lưu/hash/đọc lại trước test và kiểm hash bất biến sau test.
LightGCN refit đúng epoch, không test early stopping; ba optimizer seeds 20261103/04/05.
Optimizer repeats được trung bình trong dataset trước khi trung bình đều ba dataset seeds.

| Scenario | Seed | B1 | R |
|---|---:|---|---|
| main | 20261111 | EASE λ=1000, threshold=1 | EASE λ=1000, threshold=5 |
| main | 20261112 | LightGCN layers=2, lr=0.001, epoch=290, threshold=1 | LightGCN layers=2, lr=0.01, epoch=60, threshold=5 |
| main | 20261113 | EASE λ=1000, threshold=1 | EASE λ=1000, threshold=5 |
| sparse | 20261111 | EASE λ=1000, threshold=1 | EASE λ=1000, threshold=1 |
| sparse | 20261112 | LightGCN layers=2, lr=0.01, epoch=50, threshold=1 | LightGCN layers=2, lr=0.01, epoch=50, threshold=1 |
| sparse | 20261113 | LightGCN layers=2, lr=0.01, epoch=100, threshold=1 | LightGCN layers=2, lr=0.01, epoch=100, threshold=1 |

Main 20261112 chọn cả cấu hình/epoch khác giữa B1 và R: delta là so hệ thống joint
selection, không cô lập tác động của routing. Cần ablation giữ cùng cấu hình ở mốc sau.
Cả ba sparse seeds chọn R trùng B1; prediction files tương ứng giống từng byte.

## Kết quả từng dataset

| Scenario | Seed | Target users | B1 nDCG@10 | R nDCG@10 | Delta R−B1 | Elapsed seconds |
|---|---:|---:|---:|---:|---:|---:|
| main | 20261111 | 4921 | 0.012281090 | 0.012762106 | 0.000481016 | 2040.18 |
| main | 20261112 | 4935 | 0.014114472 | 0.013727052 | -0.000387419 | 2160.20 |
| main | 20261113 | 4919 | 0.012381678 | 0.013292433 | 0.000910755 | 2060.18 |
| sparse | 20261111 | 4923 | 0.010693366 | 0.010693366 | 0.000000000 | 2060.18 |
| sparse | 20261112 | 4931 | 0.011049580 | 0.011049580 | 0.000000000 | 2280.33 |
| sparse | 20261113 | 4925 | 0.010060565 | 0.010060565 | 0.000000000 | 2040.18 |

Elapsed gồm gate, tuning, refit, evaluation, diagnostics, I/O và polling tối đa 20 giây
cuối child process; đây không phải số đo riêng training, peak RAM hoặc serving latency.

## Trung bình đều ba dataset seeds

Các std dưới đây là sample std giữa ba dataset means, sau khi đã gộp optimizer seeds.
Không gộp main+sparse thành primary win. Family rows dùng selection threshold1 riêng.

### main

| Method | Mean nDCG@10 | Dataset std | Mean Recall@10 |
|---|---:|---:|---:|
| B1 | 0.012925747 | 0.001030694 | 0.016370128 |
| R | 0.013260531 | 0.000483263 | 0.016570033 |
| popularity | 0.003498267 | 0.000272393 | 0.004811475 |
| topic-only | 0.011028109 | 0.000297468 | 0.013394170 |
| formula | 0.005501391 | 0.000427758 | 0.006822484 |
| formula-v2 | 0.005817985 | 0.000525305 | 0.007264029 |
| ease | 0.012447980 | 0.000208119 | 0.015669368 |
| ppr | 0.010293460 | 0.000668336 | 0.013018457 |
| lightgcn | 0.013670212 | 0.000428759 | 0.017401512 |

R−B1 mean 0.000334784; sample std 0.000661326; min -0.000387419; max 0.000910755; win/tie/loss=2/0/1.

### sparse

| Method | Mean nDCG@10 | Dataset std | Mean Recall@10 |
|---|---:|---:|---:|
| B1 | 0.010601171 | 0.000500912 | 0.013610976 |
| R | 0.010601171 | 0.000500912 | 0.013610976 |
| popularity | 0.003616857 | 0.000536820 | 0.004908575 |
| topic-only | 0.004555654 | 0.000342924 | 0.006438055 |
| formula | 0.004244103 | 0.000747093 | 0.005575130 |
| formula-v2 | 0.004711142 | 0.000615215 | 0.005862408 |
| ease | 0.011231291 | 0.000506981 | 0.014017699 |
| ppr | 0.007949836 | 0.000405986 | 0.010086212 |
| lightgcn | 0.010676852 | 0.000537614 | 0.013818803 |

R−B1 mean 0.000000000; sample std 0.000000000; min 0.000000000; max 0.000000000; win/tie/loss=0/3/0.

LightGCN family threshold1 có main test mean cao nhất trong các rows này; đó là
mô tả kết quả đã quan sát, không đổi selection B1/R bằng test và không tạo claim
statistical superiority. V1/V2 dùng thêm follows/comments trong warm score; V1 tags=1,
V2 historical tags=0. Đây là so hệ thống khác tín hiệu, không hoàn toàn cùng input.

## Cohort main và sparse C3

| Cohort main | Target users seeds11/12/13 | Mean delta R−B1 | Loss budget |
|---|---|---:|---|
| C0 | 332/335/334 | 0.000000000 | PASS ≥−0.002 |
| C1 | 325/327/340 | 0.000000000 | PASS ≥−0.002 |
| C2 | 337/336/338 | 0.000000000 | PASS ≥−0.002 |
| T1 | 409/359/403 | 0.001508894 | PASS ≥−0.002 |
| T2 | 1006/1018/994 | 0.001114060 | PASS ≥−0.002 |
| W1 | 1996/2041/2014 | -0.000003689 | PASS ≥−0.002 |
| W2 | 516/519/496 | -0.000198901 | PASS ≥−0.002 |

Sparse C3 target users 704/675/671; mean R−B1=0, loss budget PASS. Đây là
R trùng B1 ở cả ba seeds, không chứng minh robustness trước topic noise hoặc
an toàn serving/authorization. Các no-target counts, arrivals và exclusions nằm đầy đủ
trong gate/report từng dataset; không xóa denominator để che cold-pool retrieval misses.

## Secondary: mean đều cohort và diagnostics

Mean đều các cohort có target trong từng dataset, sau khi gộp optimizer; rồi mean đều ba dataset. Main gồm C0/C1/C2/T1/T2/W1/W2; sparse gồm C0/C3/T1/T2/W1/W2. Cohort rỗng là N/A. Đây là secondary metric, không thay primary user-macro gate.

| Method | Main mean cohort nDCG@10 | Sparse mean cohort nDCG@10 |
|---|---:|---:|
| B1 | 0.012370204 | 0.009812788 |
| R | 0.012715970 | 0.009812788 |
| popularity | 0.003417712 | 0.003863096 |
| topic-only | 0.011184861 | 0.004608776 |
| formula | 0.007127186 | 0.004274489 |
| formula-v2 | 0.007284341 | 0.004761024 |
| ease | 0.011790260 | 0.010150914 |
| ppr | 0.010645584 | 0.007725129 |
| lightgcn | 0.013151150 | 0.009951707 |

B1/R empty và short feeds đều 0 trên target users ở cả sáu datasets; tất cả 78 method/optimizer outputs được báo trong JSON. Bảng dưới là số cặp user–relevant-target ngoài bounded cold pool trong chính cold route của method. R threshold5 đưa thêm T1/T2 qua cold nên raw miss counts có denominator khác B1; không dùng số này để so retrieval quality đơn lẻ.

| Scenario | Seed | B1 cold target pool misses | R cold target pool misses |
|---|---:|---:|---:|
| main | 20261111 | 4436 | 10207 |
| main | 20261112 | 4256 | 10096 |
| main | 20261113 | 4436 | 10334 |
| sparse | 20261111 | 4760 | 4760 |
| sparse | 20261112 | 4464 | 4464 |
| sparse | 20261113 | 4655 | 4655 |

Feed đủ 10 bài không chứng minh retrieval recall tốt. Evaluation giữ nguyên relevant targets/full eligible denominator; không loại target ngoài pool để tăng điểm. Coverage, tuổi bài và author metrics đầy đủ trong summary/report JSON.

## Practical gate đã khóa

| Điều kiện main | Kết quả |
|---|---|
| Mean delta ≥0.001 | FAIL:0.000334784 |
| Delta ≥0 trên ít nhất 2/3 dataset seeds | PASS:2/3 |
| Không mean cohort delta <−0.002 | PASS |
| Toàn bộ practical gate | **FAIL** |

Đây là engineering thresholds, không satisfaction threshold, significance hoặc power đã xác thực.

## Optimizer hierarchy

Validation chỉ training seed 20261103. Test không chọn best optimizer.
Std dưới đây là sample std trong dataset, tách khỏi dataset std ở trên;
0 ở EASE nghĩa là deterministic single fit, không phải đo ba optimizer repeats.

| Scenario | Seed | B1 optimizer std | R optimizer std | LightGCN family optimizer std |
|---|---:|---:|---:|---:|
| main | 20261111 | 0.000000000 | 0.000000000 | 0.000759224 |
| main | 20261112 | 0.000334740 | 0.000105696 | 0.000334740 |
| main | 20261113 | 0.000000000 | 0.000000000 | 0.000829425 |
| sparse | 20261111 | 0.000000000 | 0.000000000 | 0.000450641 |
| sparse | 20261112 | 0.000590153 | 0.000590153 | 0.000590153 |
| sparse | 20261113 | 0.000328222 | 0.000328222 | 0.000328222 |

Optimizer values đầy đủ trong summary.json và per-dataset report.json.
Không coi 9 optimizer runs là 9 datasets; ba dataset seeds chỉ mô tả ổn định.

## Search thực tế và verification

| Scenario | Seed | Validation records | Terminal epochs grid0/1/2/3 | V2 trials |
|---|---:|---:|---|---:|
| main | 20261111 | 196 | 300/130/300/120 | 12 |
| sparse | 20261111 | 206 | 300/190/300/110 | 12 |
| main | 20261112 | 190 | 300/110/300/110 | 12 |
| sparse | 20261112 | 184 | 300/140/240/110 | 12 |
| main | 20261113 | 192 | 300/120/290/120 | 12 |
| sparse | 20261113 | 200 | 300/140/230/200 | 12 |

Sáu preflight source inventories/dataset manifests/ZIP receipts và toàn bộ members
khớp SHA256 trước ranking. Mỗi run kiểm data integrity/cohort gate, 130 dependency lock,
common population, gate denominator, frozen selection và result manifest readback.
Inventory được so sau từng run, không source drift.

Independent audit PASS 78 result records: tính lại mọi per-user nDCG/Recall từ ranked IDs,
kiểm cohort means/users, common population, selected-record membership, hierarchy delta/std
và byte equality khi B1/R choices giống nhau. Không chạy lại model để kiểm số học.

## Artifacts và tái lập

`summary.json`, `batch.json`, `audit.json`, `source-inventory.json`, `archive.json` và
6 thư mục report/selection/tuning/manifest được tracked cạnh báo cáo. Script snapshots
ở `scripts/` là bản lưu của các scripts chạy từ `.tools/benchmarks/adaptive-confirmatory-ranking-20261108`;
khôi phục đúng đường dẫn đó nếu replay, chọn output mới, không chạy đè evidence. Bản tracked summarize.py chuẩn hóa một dòng trắng cuối file; ZIP giữ nguyên bytes script đã thực thi, checksum của ZIP receipt áp dụng cho bản archived.

CLI từ solution `Favi-BE/Favi-BE`:

```powershell
.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_adaptive_benchmark.py --dataset .tools/datasets/adaptive-confirmatory-main-20261111 --output <fresh-output>
```

Batch CLI và absolute paths đầy đủ trong batch.json. Chạy tuần tự sáu tên đã ghi;
dừng batch nếu fail, giữ failed artifacts. Config mặc định là frozen adaptive-v1.json.
Không rerun confirmatory ranking chỉ để tìm kết quả đẹp hơn.

Raw per-user results/logs/source snapshot ở solution `.tools/benchmarks/adaptive-confirmatory-ranking-20261108`.
ZIP portable `.tools/dataset-archives/adaptive-confirmatory-ranking-20261108.zip`: 225 members,
28,774,737 bytes, toàn bộ member SHA256 readback PASS. ZIP SHA256:
`d9c113d50ad147db42b092f990b46a3879d2a65cca3f082fe0b5f467629aa102`.
Source ZIP trước chạy SHA256 `2ba8afb23c9eec1b9ab1856109b70feb133cb55806a99b4355a854bebe00a371`.
Raw/ZIP không đưa vào Git. Tracked manifests giữ per-user file hashes; ZIP receipt giữ
all member hashes. Protocol archived vẫn giữ văn bản trạng thái trước confirmatory;
không sửa frozen protocol sau quan sát test.

## Giới hạn và công việc còn lại

Synthetic observed-positive replay không chứng minh causal engagement, trải nghiệm thật,
novelty hoặc statistical superiority. C3 equality phản ánh lựa chọn R=B1, không thay thế
noisy/independent sidecar robustness. Bounded cold pool có thể bỏ sót relevant targets;
report giữ cold_target_pool_misses và target denominator gốc.

JSON giữ coverage, empty/short feeds, author diversity/max share, item age, refit seconds
và model state bytes. Các số đó chưa là peak process RAM, toàn bộ training cost hoặc HTTP latency.
Chưa kiểm SQL/privacy/hydration/JWT/gateway thực tế từ ranking replay này.

Mốc kế tiếp: informative/noisy/independent replay và ablations giữ selection chính,
không retune; sau đó full cost và HTTP lifecycle/fallback/privacy/resource gates.
Không merge/deploy hoặc đổi method để gọi cùng holdout này là confirmatory mới.

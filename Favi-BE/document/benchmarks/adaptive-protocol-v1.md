# Protocol adaptive-v1 — thiết kế khóa ngày 08/11/2026

Protocol ID adaptive-v1-20261108; config máy đọc:
`tools/oasis-smoke/configs/recommenders/adaptive-v1.json`.
Bản này thay draft 04/11 và revision 05/11; không sửa benchmark lịch sử.
Thiết kế và grid khóa trước confirmatory data. Execution gate còn yêu cầu
runner tests/source inventory/dependency lock/archive readback; chưa gọi gate
PASS chỉ vì config tồn tại. Related work: [bảng đối chiếu](adaptive-related-work.md).

## Câu hỏi và đối chứng

RQ chính: chọn ngưỡng chuyển topic/general sang collaborative trong {1,5} có
lợi ích thực dụng so với chuyển ở positive đầu tiên không? Baseline B1 chọn
model/parameters/epoch tại threshold1; router R chọn chung threshold/model/
parameters/epoch từ cùng grid. Threshold1 là tập con. Cả hai chọn theo toàn bộ
validation users có target, không chỉ warm users; không tái dùng selection từ
evaluator likes-only cũ thiếu arrival-aware filtering.

Đối chứng bổ sung: raw Popularity, topic-only cho mọi user, Formula V1 source
parity chuyển tại 1 positive, Formula V2 tuned chuyển tại 1. Báo cả từng family
EASE/PPR/LightGCN threshold1, dù B1 không chọn. CF fit chỉ dùng likes; cold
fallback nhận observed topics giống nhau. Formula còn dùng follows/comments:
đây là so hệ thống khác tín hiệu, không phép so thuật toán cùng input tuyệt đối.

V1 dùng observed single-tag count=1 theo ADAPTIVE_OFFLINE_CONTRACT.md. V2 giữ
đúng heuristic lịch sử FORMULA_V2_BENCHMARK.md: tag count=0,
Q=log10(1+1.1*likes+2*comments), affinity A lịch sử,
score=(social_weight*A+1.5*Q)*exp(-decay*ageHours); không V1 damping;
cap null hoặc hard cap2 không relax. Topics chỉ dùng cho cold fallback.
Đây là khác biệt khai báo trước test; V2 không có claim production parity.

## Dữ liệu, thời gian và gates

Ba seed chưa quan sát 20261111/20261112/20261113, mỗi seed main+sparse-topic,
5.000 users/2.000 maximum posts/500 ticks/10 topics. Generator lifecycle-v2
đã pilot tại 3bb48b3; helper classify_cohort thêm sau không đổi generation.
Lưu resolved configs/CLI/source/lock/archive hashes. Pilot20261110 đã quan sát
baseline test, luôn exploratory, không gộp vào confirmatory averages.

Train prefix <300; validation request300/targets[300,400); refit <400;
test request400/targets[400,500). Tick=1 giờ. Selector chỉ nhận events <400;
features chỉ có trước request, gồm arrival/item/preference, không latent data.
Eligible chung loại own/prior-positive/future items. Targets là distinct likes
held-out giao eligible; no-target là N/A và báo riêng. Mọi trial/method cùng
candidate/target/user fingerprints ở mỗi phase. Score ties source ID tăng dần;
V1 giữ stable source-ordered input theo parity, cold giữ dedup/round-robin/
soft cap/relax/fallback. Bounded cold pool có thể bỏ sót relevant target trong
full eligible; không đổi target denominator để che retrieval loss.

C0=0 positive/0 topic; C1=0/1; C2=0/2–10; C3=0/có topic nhưng <10 matching
eligible items, ưu tiên C3 trước C1/C2. T1=1 positive; T2=2–4; W1=5–19;
W2>=20. Main yêu cầu C0/C1/C2/T1/T2/W1/W2; sparse yêu cầu C3. Mỗi nhóm >=30
target users tại cả cutoff300/400. Gate kiểm 100% referential integrity,
exposure trước feedback, timestamp/arrival/topic/ID/manifest và ZIP readback.
Confirmatory gate fail: giữ seed/log, báo thiếu, dừng benchmark batch đến khi
có revision minh bạch. Không thay seed, sửa label, chọn subset theo future
label hoặc hạ gate. Không claim bảo toàn cohort thiếu mẫu.

## Search và selection khóa trước test

Grid order theo thứ tự liệt kê, Cartesian product trái sang phải:

| Family | Grid | Budget |
|---|---|---|
| EASE | lambda [1,10,100,1000] | 4 fits, mỗi fit score thresholds [1,5] |
| PPR | alpha [.5,.85,.95] | 3 fits, mỗi fit score thresholds [1,5] |
| LightGCN | layers [1,2] × learning rate [.001,.01]; dim64/L2=.0001 | 4 trajectories seed20261103, tối đa300 epochs/check mỗi10/patience5 |
| Formula V2 | social [.5,1.5,2.5] × decay [0,.005] × cap [null,2] | 12 trials, threshold1 |

EASE/PPR fits dùng chung giữa thresholds. LightGCN checkpoint đánh giá cả hai
thresholds, lưu best epoch và stale riêng; dừng khi cả hai stale>=5 hoặc epoch300.
Tối đa30 checkpoints/trajectory, 120 states/240 threshold evaluations. Threshold
đã stale vẫn được score khi threshold kia tiếp tục. Không tuning optimizer
seed/dim/L2/patience/topic pool; không che epoch search dưới tên chỉ 4 configs.

Chọn max validation macro nDCG@10. Lấy các records cách max <=1e-12 làm tập hòa,
rồi threshold1, family EASE/PPR/LightGCN, grid index, epoch nhỏ nhất. Không dùng
Recall phá hòa. Trong family/threshold dùng nDCG rồi grid/epoch; V2 dùng nDCG
rồi grid index. B1 và R chọn độc lập trên cùng records; R không bị buộc dùng
warm config của B1. Hàm chọn phải so với global max để tránh tolerance drift.

Persist selection.json gồm cấu hình/epochs, prefix/config/source hashes trước
bất kỳ test evaluation nào, kiểm hash không đổi sau test. Refit parameters và
epoch trên prefix<400, không early stop bằng test. LightGCN test ba seeds
20261103/20261104/20261105 cho mỗi cấu hình được chọn, cache cấu hình trùng để
B1/R dùng cùng fit ở cùng seed. Không chọn best test optimizer. Validation chỉ
seed20261103 là giới hạn phải report. Không truyền events>=400 vào selector.

## Metrics và tiêu chí bác bỏ

Primary: macro user nDCG@10 trên main lifecycle users có target. Secondary:
Recall@10, từng cohort, mean đều cohort, coverage, empty/short feeds, author
diversity/max share, tuổi bài và chi phí. Sparse là scenario riêng; không gộp
users hoặc average main/sparse tạo primary win. Báo denominators, no-target,
future-target exclusions và cold pool misses.

Gộp optimizer seeds trong mỗi dataset/config trước, sau đó delta R−B1 từng
dataset và mean đều ba dataset seeds, sample std/min/max và win/tie/loss.
Không coi9optimizer runs là9datasets, không gộp validation/test requests làm
mẫu độc lập. Báo optimizer std riêng dataset std; ba seeds chỉ mô tả ổn định.

Practical gate: main mean delta>=.001 tuyệt đối, delta>=0 trên >=2/3 seeds,
không cohort main đủ mẫu nào mean delta<−.002. Đây là engineering thresholds
để loại gain quá nhỏ và tránh che thiệt hại cohort; không mức satisfaction,
statistical significance hoặc power đã xác thực. Không đạt thì bác bỏ claim
lợi ích thực dụng trong thiết kế này. Sparse C3 báo riêng, cùng loss budget.002;
fail thì không claim fallback an toàn toàn scenario. Không đổi metric sau test.

Robustness informative chính; noisy/independent sidecars dùng cùng events/
candidates/targets và cấu hình informative đã khóa, không retune. Báo overlap,
cohort theo sidecar và paired users; chuyển cohort không phải đổi population.
Bỏ topics/bỏ routing là secondary ablations giữ selection chính, không thêm
grid sau test. Exposure chưa phụ thuộc declared topics: sidecar replay không
đo tác động hành vi hay causal engagement uplift.

## Serving, provenance và giới hạn

HTTP sau offline: fresh user chọn/bỏ topic, positive đầu tiên/transition/warm,
new item/user, artifact missing/stale và quyền bài đổi sau export. Kiểm JWT,
idempotency, optimistic version, ban/NSFW/archive/delete/own/prior positives.
Giữ module boundary/facades và DI extensions. Lab: concurrency10, 100 warmup
+1000 measured requests, warm p95<=100ms, không lỗi quyền; >=5 cold-process
restarts, không claim p99 từ5mẫu. Báo direct/gateway, máy, artifact size,
load/rebuild và peak process RAM. Giữ <=1000users/16MiB/300candidates per
user-method; sample serving không được gọi là full5k deployment.

Trước confirmatory, runner tests prefix/arrival/population/frozen selection/
refit/optimizer hierarchy/V2/router ties phải pass. Lưu source/config/lock
inventory và archive readback. Sửa lỗi trước confirmatory test phải ghi revision;
sửa method sau quan sát test biến run thành exploratory và cần holdout mới.
Không tạo thêm chu kỳ quota reset. Replay synthetic không chứng minh novelty
hoặc trải nghiệm thật. Primitive parity không bao gồm SQL/HTTP; HTTP timing
không thay offline relevance. Không merge/deploy tự ý.

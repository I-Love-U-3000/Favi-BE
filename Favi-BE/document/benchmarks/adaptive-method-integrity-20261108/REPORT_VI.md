# Kiểm tra độc lập phương pháp và lựa chọn validation

08/11/2026. Chưa phát hiện lỗi phương pháp trong phạm vi mã đã rà soát.
Có một khoảng trống kiểm toán được bổ sung: audit ranking lịch sử chỉ kiểm
selected record thuộc tuning, chưa tự tính lại record thắng. Kiểm mới xác nhận
**36 lựa chọn trên cả sáu dataset đều đúng protocol**, với 1.168 validation
records. Không thay runner, selection, frozen outputs hoặc chạy lại mô hình.
Primary practical gate vẫn **FAIL**, không có bằng chứng mới về superiority.

## Đối chiếu mã với protocol

Đọc [protocol](../adaptive-protocol-v1.md),
[offline contract](../../ADAPTIVE_OFFLINE_CONTRACT.md), runner/replay contracts,
reports ranking/replay/M5/cost/readiness và các implementation sau:

| Phần | Bằng chứng trong mã | Kết quả và giới hạn |
|---|---|---|
| Ranh giới train/validation/test | `run_adaptive_benchmark.run/tune`, `InteractionData.__init__`, `Ranker.__init__` | Selector chỉ nhận events<400; fit/features<300; label validation[300,400). Persist/hash/đọc selection trước refit<400 và test[400,500); không test callback |
| Arrival và preference | `AdaptiveFeed.candidates/topics/cohort`, `visible_preferences`, `analyze` | Arrival>=cutoff không candidates; preference>=cutoff không topic. Data gate kiểm action/arrival và policy sidecars. Metadata đầy đủ có mặt trong bộ nhớ, nhưng feature accessor lọc theo request |
| Eligible và targets | `evaluate`, `AdaptiveFeed.candidates`, analyzer | Distinct held-out likes giao eligible trước request; loại own/prior-positive/future item. Cold retrieval không thu nhỏ relevant denominator. Warm model cùng cutoff/catalog; evaluator chặn ranked ID ngoài eligible |
| Macro metric và no-target | `metrics/summarize/evaluate`, analyzer | nDCG tính từng user rồi mean đều target users. Người không có eligible target là N/A; gate đếm cả users không phát sinh held-out like. `label_users` của evaluator chỉ là users có held-out like, không phải toàn bộ arrived users |
| Selection và epoch | `tune/select/DualPatience/refit` | Threshold1/5 dùng chung fit; LightGCN đánh giá mỗi10epochs, stale riêng và tiếp tục score cả hai. Chọn từ global max tolerance1e-12, threshold/family/grid/epoch; refit đúng epoch |
| Optimizer và dataset hierarchy | ranking `scripts/summarize.py`, replay `scripts/summarize_audit.py` | Gộp ba optimizer seeds trong dataset trước mean đều ba dataset; main/sparse riêng. Không chọn best test optimizer. Validation chỉ optimizer20261103 vẫn là giới hạn |
| Paired replay và ablation | `run_adaptive_replay.variants/tasks/paired_check/run` | Sidecars giữ identity/timeline, candidates/targets/user order; primary ranked IDs bằng original. No-routing giữ cấu hình/epoch R, threshold1; no-topics giữ general fallback. Sparse informative là pre-scarcity |
| Phạm vi kết luận | M5, replay, cost và readiness reports | Main mean delta+.000334784<.001; noisy/independent/no-topics âm; sparse R=B1. Main12 joint selection đổi config/epoch; không quy toàn delta cho routing. Không causal, statistical, real-user hoặc full-stack claim |

Đây là review mã có mục tiêu, không chứng minh tuyệt đối không có leakage.
Data qualification nhìn toàn dataset để kiểm integrity/cohort trước ranking theo
protocol; không phải tuning ranking trên test. Không điều chỉnh seed/gate sau
khi nhìn test. SHA256 xác nhận bytes, không tự chứng minh thứ tự thực thi lịch
sử hoặc tính đúng của thuật toán. Review source và arithmetic audits bổ sung
cho provenance, không thay thế nhau.

## Kiểm độc lập lựa chọn đã lưu

[audit_selection.py](audit_selection.py) chỉ dùng Python stdlib, không import
runner, NumPy hoặc trainer. Nó đọc config, tuning và selection tracked rồi:

1. Dựng lại đủ4EASE/3PPR configs với cả thresholds,12V2 trials và4LightGCN
   trajectories; kiểm parameters/index/epoch và không thiếu/trùng/thừa record.
2. Kiểm checkpoint liên tục mỗi10epochs, đủ hai thresholds. Tự tính strict
   improvement/stale; không được dừng trước khi cả hai stale>=5 hoặc epoch300,
   cũng không được tiếp tục sau dual patience.
3. Tính lại global maximum và tập hòa trong tolerance, rồi áp thứ tự đã khóa
   cho B1/R, ba family threshold1 và V2. Membership đơn thuần không đủ.
4. Kiểm finite nDCG[0,1] và common validation population fingerprint.

[summary.json](summary.json) là receipt của bytes workspace lúc chạy; hashes
raw có thể khác sau Git LF/CRLF normalization. Checker không lấy receipt hashes
làm baseline portability và không ghi lại tuning/selection. CI tính lại từ
checkout hiện tại, tạo receipt mới trong `.tools/evidence-ci`; không so raw
hash receipt Windows với Linux. Receipt không xác thực dataset/raw predictions;
phần đó thuộc archived arithmetic/readiness audit đã có.

Từ solution directory:

```powershell
.tools/oasis-env/Scripts/python.exe -I -B -X utf8 -m unittest discover -s document/benchmarks/adaptive-method-integrity-20261108 -p test_selection.py -v
.tools/oasis-env/Scripts/python.exe -I -B -X utf8 document/benchmarks/adaptive-method-integrity-20261108/audit_selection.py --output <fresh-receipt.json>
```

7tests PASS: đủ sáu dataset; lựa chọn có membership nhưng suboptimal bị chặn;
missing/duplicate/wrong grid bị chặn; thiếu threshold/checkpoint hoặc dừng sớm
bị chặn; tolerance chọn từ global maximum thay vì drift theo incumbent;
tiếp tục sau dual patience, nDCG không hữu hạn và population drift bị chặn.
Checker dùng explicit exceptions, không mất validation khi Python optimize.
Workflow `adaptive-evidence.yml` chạy trên Windows/Linux khi đổi checker,
tuning/selection hoặc config. Hosted status chỉ được kết luận sau quan sát CI.

Không phát hiện lỗi cần sửa frozen method. Không cần chuyển các kết quả thành
exploratory chỉ vì bổ sung audit; nếu phát hiện method defect sau test trong
tương lai, phải khai báo tác động theo protocol, không sửa âm thầm lịch sử.
Serving250users, gateway429FAIL, missing search-only/training peak RAM và các
giới hạn clean-machine/user-study giữ nguyên. Không có numerical/HTTP rerun.

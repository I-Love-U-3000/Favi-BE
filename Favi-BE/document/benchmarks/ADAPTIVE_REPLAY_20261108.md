# Fixed-selection robustness và ablation replay

Runner `tools/oasis-smoke/run_adaptive_replay.py` bổ sung secondary analysis cho
protocol adaptive-v1-20261108. Không sửa original runner, selection hay test holdout.
Primary practical gate đã FAIL; replay không tạo cơ hội chọn lại phương pháp.

Input bắt buộc: qualified dataset và thư mục ranking có manifest, source inventory,
selection, protocol, report và per-user predictions. Runner kiểm mọi file trong
ranking manifest, source của original runner/feed/models/evaluator, dependency lock,
data gate, prefix fingerprint và onboarding hash liên kết selection trước refit.
Output phải mới, ngoài hai thư mục input. Ghi inputs/source/progress/report/manifest,
kiểm original evidence bất biến và readback toàn bộ output sau chạy.

Các mode: `primary` dùng onboarding.json đã chọn model; `informative`, `noisy`,
`independent` dùng đúng sidecars dataset đã sinh. `no-topics` xóa topics từ primary,
giữ IDs/thứ tự/arrival/preference timestamps. Tất cả sidecars phải giữ identity và
timeline; `analyze` kiểm chúng khớp deterministic generation policy.

Main primary bằng informative. Sparse primary ép topic music khan hiếm; sidecars
lưu metadata trước bước scarcity, vì vậy sparse informative không đồng nghĩa primary
và có thể chuyển C3 sang C1/C2. Không sửa sidecar để giữ C3 hay loại users đổi cohort.
Report overlap của topics so với primary trên paired target users, transitions
primary->sidecar, metrics theo cả primary cohorts lẫn current cohorts. Overlap là
số topic chung chia declared slots mới; no-topics có denominator0 nên N/A.

Trong primary và ba sidecars chạy B1/R, selected family references, selected V2,
Popularity/topic-only/Formula V1. Primary predictions phải khớp từng ranked ID với
original output cho mọi method/optimizer seed. So candidate hashes, relevant IDs
và user order trong mọi replay, đồng thời so population fingerprint original.
Không dùng cold bounded pool để thu nhỏ relevant denominator.

Ablations secondary chỉ dùng primary metadata/selection: `B1` và `R` trong mode
no-topics vẫn dùng general cold fallback; `R-no-routing` đặt threshold=1 nhưng giữ
nguyên family/parameters/epoch R. So R với R-no-routing cô lập threshold trên cùng
model; so R-no-routing với B1 mô tả khác biệt cấu hình đã joint-select. Không xóa
cold fallback hoặc ép collaborative cho zero-positive users.

Refit chỉ events<400, exact epochs selection, optimizer seeds20261103/04/05 nếu
LightGCN, deterministic single fit nếu EASE/PPR. Cache theo family/parameters/seed
được chia sẻ giữa tất cả mode/labels. Không gọi tune hoặc test early stop. Khi tổng
hợp phải gộp optimizer trong dataset trước, main và sparse riêng, không coi các
sidecars là independent datasets. Refit seconds chưa gồm full training cost/peak RAM.

CLI từ solution, output mới mỗi lần:

```powershell
.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/run_adaptive_replay.py --dataset <dataset> --ranking <original-ranking> --output <fresh-output>
```

Meaningful tests kiểm sidecar timeline, paired drift, giữ joint parameters/epochs,
orchestration thật trên numerical LightGCN fixture với primary prediction parity,
ba optimizer seeds/exact10epochs và shared refit cache, manifest tampering,
output overwrite rejection. Gate/lock
được mock trong fixture nhỏ; không gọi fixture đó là qualified 5k dataset gate.
Đây synthetic metadata replay, không behavioral/causal uplift, SQL hoặc HTTP parity.

# Execution outcome — 08/11/2026

[Cả sáu confirmatory ranking runs](adaptive-confirmatory-ranking-20261108/REPORT_VI.md)
đã hoàn tất/PASS technical checks. Main practical gate FAIL: mean R−B1 nDCG@10
+0.000334784 <0.001, dù2/3seeds dương và cohort loss budget PASS. Sparse R=B1,
C3delta0; independent audit PASS78result records và ZIP225members readback.
Không runner/config changes hoặc retune trong execution. Robustness/ablations,
full cost và HTTP gates vẫn pending. Những trạng thái chưa chạy bên dưới là
snapshot implementation/generation cũ, đã được execution report này thay thế.
# Common-candidate adaptive runner — 08/11/2026

Cập nhật sau milestone generation: [cả sáu confirmatory datasets](adaptive-confirmatory-data-20261108/REPORT_VI.md)
đã PASS integrity/cohort/ZIP readback; runner benchmark chưa chạy. Những câu
"chưa sinh" bên dưới mô tả trạng thái tại checkpoint implementation trước đó.

Implementation: tools/oasis-smoke/run_adaptive_benchmark.py; protocol adaptive-v1-20261108.
Không thay production API, database hoặc benchmark cũ.

Runner kiểm scenario/integrity bằng analyzer và dependency lock trước output.
Chỉ nhận config frozen, 500 ticks và output mới ngoài dataset. Selector nhận
 events <400; fit <300. AdaptiveFeed áp arrival/preference visibility và eligible
catalog chung. Mỗi trial kiểm users/candidates/targets fingerprint và gate denominator.

EASE/PPR fit một lần mỗi cấu hình và score thresholds1/5. LightGCN giữ hai
patience counters, score cả hai mỗi10epochs; bỏ callback epoch1 của trainer cũ.
Strict nDCG improvement reset counter; dừng khi cả hai stale>=5. Giữ toàn bộ
checkpoint records để chọn global max với tolerance1e-12, rồi threshold/family/
grid/epoch. Không dùng Recall phá hòa. Lưu epoch; không giữ mọi validation
embedding vì test refit deterministic từ đầu theo epoch đã chọn.

B1, R và mỗi family threshold1 chọn riêng. V2 có12trials, giữ Ranker lịch sử
zero-tag/affinity/quality/decay/hard cap không relax; cold fallback tại0positive.
V1 vẫn observed single-tag và primitive đã parity.

Trước test, selection.json lưu choice, prefix/config/onboarding/source hashes;
đọc lại file rồi refit <400. LightGCN dùng đúng epoch, không test callback;
chạy ba optimizer seeds. Cache family/parameters/seed chia sẻ giữa B1/R/family.
Cuối test kiểm selection hash không đổi. Aggregation helper mean optimizer
trong dataset trước khi mean/std giữa datasets; main/sparse tách ở batch report.

Artifacts: tuning, frozen selection, protocol/config gốc, source/tests, lock,
per-user test JSONL, per-cohort report, SHA256 manifest đọc lại. Diagnostics trên
target users gồm empty/short, tuổi bài, relevant targets ngoài bounded cold pool;
không đổi target denominator. Gate giữ số no-target. Model bytes/refit seconds
không phải peak process RAM hay HTTP latency. Robustness/ablations, practical
gate giữa datasets và serving/cost lab là milestone sau, chưa claim superiority.

9 tests mới kiểm global-max tolerance chống drift, tie hierarchy, dual patience,
prefix rejection, arrival boundary, population equality, V2 historical semantics,
numerical tune/refit EASE/PPR/LightGCN thật trên fixture, fixed refit epoch,
optimizer hierarchy, frozen selection/cache và manifest readback. Orchestration
fixture mock scenario gate/lock/tuning để kiểm lifecycle; không gọi đây là
end-to-end production/data gate. Numerical fixture riêng dùng trainer thật với
dim4/max20 để kiểm giao diện, không đổi frozen grid.

Chưa sinh/quan sát confirmatory seeds20261111/12/13. Không rerun pilot ranking
hoặc primitive parity đã hoàn tất. Execution readiness cần inventory thực tế và
regression; dataset ZIP readback vẫn bắt buộc với từng dataset được sinh.

Command từ tools/oasis-smoke: Python environment tại solution
.tools/oasis-env/Scripts/python.exe -X utf8 -m unittest test_adaptive_benchmark -v.
Numerical CLI ngoài sandbox theo handover để tránh Windows DLL popups.

Verification cuối milestone: toàn bộ111Python tests PASS (10.840s), gồm9tests
runner mới. Dependency lock130packages khớp. Inventory16files và ZIP source
readback PASS tại .tools/benchmarks/adaptive-runner-readiness-20261108/readiness.json.
Source ZIP SHA256: 5142e8a93fcdd8a0d1b17774822dc0cd7f8cb8b01f5b1c4e946bc0e3c50a6827.
Đây là runner readiness; dataset gates/ZIP readback phải chạy khi sinh từng seed.

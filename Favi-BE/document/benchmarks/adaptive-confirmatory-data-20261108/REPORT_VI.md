Execution tiếp theo đã hoàn tất: xem [confirmatory ranking report](../adaptive-confirmatory-ranking-20261108/REPORT_VI.md).
Sáu runs PASS technical checks nhưng main practical gain gate FAIL. Các câu
chưa chạy ranking trong báo cáo generation dưới đây mô tả thời điểm chốt data gate.
# Confirmatory lifecycle-v2: data gate PASS

08/11/2026, Asia/Saigon. Protocol adaptive-v1-20261108. Cả sáu dataset
main/sparse-topic × seeds 20261111/20261112/20261113 đã PASS integrity,
cohort tại cutoff 300/400 và ZIP readback. Không seed thay thế, không hạ
gate, không có lượt thất bại trong batch. Chưa chạy ranking/tuning/test model.

Mỗi dataset: 5.000 users, 500 ticks, 2.000 bài thực tế; logical arrival
lifecycle-v2, public OASIS actions tuần tự, native accounts tại bootstrap.
Main cần C0/C1/C2/T1/T2/W1/W2; sparse cần C3. Mọi ô bắt buộc >=30 users
có target. C3 main và những nhóm khác sparse là N/A về gate, không bị xóa
khỏi JSON. Users không target, target exclusions và sidecar overlap giữ đủ.

## Cohort có target

| Seed | Scenario | Cohort | Validation | Test | Gate |
|---|---|---|---:|---:|---|
| 20261111 | main | C0 | 335 | 332 | PASS |
| 20261111 | main | C1 | 328 | 325 | PASS |
| 20261111 | main | C2 | 355 | 337 | PASS |
| 20261111 | main | T1 | 298 | 409 | PASS |
| 20261111 | main | T2 | 354 | 1006 | PASS |
| 20261111 | main | W1 | 1319 | 1996 | PASS |
| 20261111 | main | W2 | 216 | 516 | PASS |
| 20261111 | sparse-topic | C3 | 653 | 704 | PASS |
| 20261112 | main | C0 | 336 | 335 | PASS |
| 20261112 | main | C1 | 344 | 327 | PASS |
| 20261112 | main | C2 | 340 | 336 | PASS |
| 20261112 | main | T1 | 295 | 359 | PASS |
| 20261112 | main | T2 | 341 | 1018 | PASS |
| 20261112 | main | W1 | 1340 | 2041 | PASS |
| 20261112 | main | W2 | 208 | 519 | PASS |
| 20261112 | sparse-topic | C3 | 676 | 675 | PASS |
| 20261113 | main | C0 | 349 | 334 | PASS |
| 20261113 | main | C1 | 333 | 340 | PASS |
| 20261113 | main | C2 | 336 | 338 | PASS |
| 20261113 | main | T1 | 317 | 403 | PASS |
| 20261113 | main | T2 | 320 | 994 | PASS |
| 20261113 | main | W1 | 1325 | 2014 | PASS |
| 20261113 | main | W2 | 216 | 496 | PASS |
| 20261113 | sparse-topic | C3 | 675 | 671 | PASS |

## Dataset và archive

| Seed | Scenario | Likes | Comments | Events | Generation seconds | ZIP bytes |
|---|---|---:|---:|---:|---:|---:|
| 20261111 | main | 66659 | 10891 | 347105 | 520.297 | 31417876 |
| 20261111 | sparse-topic | 66382 | 10982 | 347229 | 509.594 | 31365271 |
| 20261112 | main | 67370 | 11070 | 348770 | 517.391 | 31711834 |
| 20261112 | sparse-topic | 66455 | 11043 | 347213 | 514.109 | 31402783 |
| 20261113 | main | 66836 | 10957 | 346447 | 511.703 | 31436863 |
| 20261113 | sparse-topic | 66108 | 11033 | 346353 | 562.0 | 31283194 |

Thời gian là elapsed generation/audit/export từng lượt chạy tuần tự trên máy
hiện tại, không phải benchmark tốc độ hay đầy đủ chi phí model/serving.

Mỗi ZIP có 32 members, tất cả được đọc lại và so SHA256 với manifest.
Checksum đầy đủ trong từng `*-archive.json`; events/config/source hashes,
130 dependency versions, CLI tuyệt đối và source inventory trong
[generation.json](generation.json). Source ZIP readback PASS; inventory
được so lại sau từng dataset, không có source drift.

## Tái lập và vị trí

Chạy từ solution `Favi-BE/Favi-BE`, bằng `.tools/oasis-env/Scripts/python.exe`:

```powershell
.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/generate_dataset.py --config tools/oasis-smoke/configs/adaptive-lifecycle-v2.json --seed 20261111 --adaptive --arrival-policy lifecycle-v2 --scenario main --output .tools/datasets/adaptive-confirmatory-main-20261111
```

Dùng đúng ba seed đã liệt kê và scenario main/sparse-topic. Output phải mới;
không chạy đè các bundle đã sinh. Sau generation, analyzer `analyze(root)`
kiểm lại manifest/integrity/policy/cohorts và `package_dataset.package` tạo
ZIP mới ngoài dataset, kiểm từng member. Script batch đã freeze trong
`.tools/benchmarks/adaptive-generation-20261108/frozen-source.zip`.

Raw datasets: `.tools/datasets/adaptive-confirmatory-{main|sparse}-{seed}`.
ZIP và receipts: `.tools/dataset-archives/adaptive-confirmatory-{main|sparse}-{seed}.zip`
và `.zip.json`. Logs/gate reports/source ZIP: `.tools/benchmarks/adaptive-generation-20261108`.
Raw/ZIP không đưa vào Git. Tracked report giữ đầy đủ gate/provenance.

## Giới hạn và bước kế tiếp

Đây là synthetic readiness, không chứng minh relevance, superiority,
statistical power hoặc hành vi người thật. Main/sparse là hai scenario
riêng, không gộp thành primary average. Các dataset đã được kiểm cohort
nhưng chưa chấm model; không được sửa phương pháp theo test ranking sau này
mà tiếp tục gọi cùng holdout là confirmatory.

Mốc tiếp: common-candidate benchmark với frozen selection validation-only,
refit exact epochs và optimizer hierarchy theo protocol. Robustness/ablations,
main-only practical gate, full cost và HTTP serving còn chưa thực hiện.
Không thay code/generator trong milestone generation; không rerun primitive
parity/pilot hoặc 111 regression tests đã PASS tại commit 2ea65f8 vì không có
thay đổi source mới. Kiểm chứng thực tế là sáu lần integrity/cohort/archive.

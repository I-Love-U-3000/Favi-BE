# Favi adaptive: đọc tiến độ và kết quả

Cập nhật08/11/2026: ranking và robustness đã có bằng chứng; primary practical
relevance FAIL. HTTP lifecycle/SQL PASS, direct lab latency PASS, gateway load
FAIL429. [Kiểm toán chi phí](benchmarks/adaptive-cost-audit-20261108/REPORT_VI.md)
xác nhận sáu lượt gốc12641.235283s,33unique refits199.635498s. Search-only time
và peak RAM training gốc không được ghi; audit hoàn tất với khoảng trống đã nêu,
không coi M4 toàn bộ PASS. Các ghi chú chưa chạy trong phần lịch sử bên dưới
được thay thế bởi reports ranking/replay/HTTP/cost mới nhất. Tiếp theo M5: gói
kết luận khóa luận, bảng claim→evidence và giới hạn, không retune cứu kết quả.

Kế hoạch điều hành hiện tại: [final revision 05/11/2026](ADAPTIVE_FINAL_REVISION.md).
Thay cách vá từng cohort bằng ma trận scenario và gate tổng trước benchmark.

Mục tiêu: người mới có feed hữu ích; khi có lịch sử, hệ thống chuyển sang cá nhân hóa đúng lúc. Đo được lợi ích, chi phí và điều kiện thất bại để làm khóa luận có thể bảo vệ.

## M1 — Dữ liệu đúng câu hỏi

Đầu ra: simulator có lịch bắt đầu hoạt động, sở thích khai báo có thời điểm, đúng 10 chủ đề Favi; mọi hành động qua OASIS thật và được audit. Đạt khi không hành động trước arrival, không đọc sở thích tương lai, manifest và export hợp lệ. Chưa cần model mới.

Ngày 05/11/2026: đã thêm `--adaptive` vào generator, config `adaptive-pilot.json` và policy riêng. V1 mặc định giữ nguyên. Native accounts provision ở bootstrap; logical arrival điều khiển thời điểm hoạt động. Bootstrap follows chỉ nối người có mặt lúc bootstrap, bootstrap posts chỉ từ creators đã có mặt; chưa bổ sung follows bù cho người đến sau. Khác biệt này là một phần exposure policy cần phân tích.

Topics khai báo được chọn ngẫu nhiên độc lập latent interest: chỉ dùng để thử plumbing/stress, chưa phù hợp làm thí nghiệm chính về lợi ích onboarding chính xác. Cần thêm chế độ informative/noisy trước confirmatory run. Sidecar `onboarding.json` được manifest hash; không giả định preference đã được import vào API production. `adaptive-validation.json` kiểm tra mọi actor và follow target đã đến thời điểm hoạt động.

95 tests regression pass sau tích hợp. Pilot native 1.000 users/30 ticks hoàn tất trong 47,984 giây: 45 posts, 898 likes, 647 follows, 191 comments; generator audit pass. Đây là smoke, không thay thế protocol 5k/500 ticks. Raw: `.tools/datasets/adaptive-pilot-20261105-001`.

Bảng lịch sử [pilot JSON](benchmarks/adaptive-pilot-20261105.json) tại tick 18/24 đã có cold và transition; W1 chỉ 5/6 users, W2 không có. Bảng này chưa kiểm tra targets và chưa tách C3. M2 chưa đạt: cần pilot dài hơn và analyzer đầy đủ trước confirmatory benchmark.

## M2 — Đủ tình huống để so sánh

**Trạng thái mới nhất 05/11/2026: pilot data gate PASS** theo
[revision tổng thể đã chạy](benchmarks/adaptive-lifecycle-v2-20261105/REPORT_VI.md).
Cả bảy nhóm main và C3 scenario riêng đủ >=30 target users ở cả hai cutoff.
98 regression tests pass; chưa có model results hoặc confirmatory freeze.
Các trạng thái FAIL bên dưới là lịch sử các candidate trước revision.

[Cold waves pilot](benchmarks/adaptive-cold-waves-20261105.md): C0/C1/C2 có
92–97 users có target mỗi phase, đạt ngưỡng cold; W1/W2 cũng đủ. T1/T2 còn
thiếu validation, C3 chưa có. Gate toàn bộ chưa pass; tiếp theo thêm transition
wave và scenario thiếu nội dung riêng. 97 regression tests pass.

Cập nhật 05/11/2026: [pilot 500 ticks](benchmarks/adaptive-pilot-long-20261105.md)
đã chạy và read-back pass; 96 regression tests pass. Warm đủ nhưng cold và C3
thiếu mẫu nên gate FAIL, M2 chưa hoàn tất. Bước tiếp theo là arrival waves trên
pilot và scenario thiếu nội dung riêng, không chạy benchmark chính sớm.

Đầu ra: bảng số người không lịch sử, 1 positive, 2–4, 5–19 và >=20; chia thêm chủ đề và thiếu bài. Phải báo cả người không có target. Pilot nhỏ cho biết thiếu nhóm nào; sửa cơ chế arrival trên pilot rồi khóa thiết kế, không chọn dữ liệu theo điểm model. Không tự bảo đảm 30 users mỗi cohort khi chưa đo. Cần audit independent khi đọc lại bundle, không chỉ tin marker do generator ghi.

## M3 — Kết quả chọn phương pháp

Cập nhật 08/11/2026: protocol và common-candidate runner đã khóa/kiểm chứng
tại 2ea65f8 (111 tests PASS). [Sáu dataset confirmatory](benchmarks/adaptive-confirmatory-data-20261108/REPORT_VI.md)
main/sparse-topic × seeds20261111/12/13 PASS integrity/cohort/ZIP readback.
Không thay seed/hạ gate; chưa chạy ranking/tuning model trên các dataset này.
M3 chưa hoàn tất: tiếp theo chạy benchmark validation-only theo frozen protocol,
sau đó robustness, main-only practical gate và báo hierarchy optimizer/dataset.

Đầu ra: cùng dữ liệu và cùng tiêu chí, so Popularity, công thức Favi, EASE/PPR/LightGCN và chính sách chuyển ở 1 hoặc 5 positives. Chọn cấu hình bằng validation, test chỉ dùng đánh giá. Đạt khi tái lập được, không leakage, đối chứng công bằng. Kết luận có thể là không cần router mới.

## M4 — Feed thật và chi phí

Đầu ra: HTTP demo từ cold đến warm và fallback khi artifact lỗi/hết hạn, cùng số liệu latency/memory/rebuild. Đạt khi đúng quyền truy cập và đạt budget lab đã chốt; không gọi đó là production SLA.

## M5 — Gói khóa luận

Đầu ra: báo cáo nối từng kết luận đến raw results, source/config hashes, biểu đồ chất lượng theo lượng lịch sử và chi phí. Ghi phản ví dụ và hạn chế synthetic. Không tuyên bố tác động với người thật từ simulator replay.

## Mẫu báo cáo mỗi lượt

1. Đã làm gì: thay đổi và phạm vi cụ thể.
2. Đã kiểm chứng gì: command, pass/fail, số mẫu và artifact.
3. Kết quả chứng tỏ gì: một kết luận có giới hạn.
4. Còn thiếu gì: gate nào chưa đạt, không che cohort thiếu mẫu.
5. Tiếp theo làm gì: action trực tiếp theo milestone.

Không dừng chờ review thường lệ theo yêu cầu người dùng. Báo đúng trạng thái, không đánh dấu milestone hoàn tất chỉ vì có code hoặc tests đơn vị.

# Pilot dài: warm đã có, cold còn thiếu

Ngày 05/11/2026. Mục đích: kiểm tra dữ liệu, chưa chấm điểm recommender.

## Đã chạy

OASIS native, 1.000 users, 500 ticks, 10 topics; seed 20261110, 300 initial posts, tối đa 500 posts. Hoàn tất trong 106,344 giây: 500 posts, 15.089 likes, 2.160 follows, 3.020 comments, 60.956 exposures. Audit generator và read-back 27 manifest files pass. 96 tests regression pass.

Config: `tools/oasis-smoke/configs/adaptive-pilot-long.json`. Raw: `.tools/datasets/adaptive-pilot-long-20261105-001`. Source chưa commit; đây là kết quả exploratory, không phải confirmatory evidence. [JSON đầy đủ](adaptive-pilot-long-cohorts-20261105.json) chứa events hash và denominator.

## Số người có target hợp lệ

Target là bài được like trong cửa sổ sau request, đã tồn tại tại request, không phải bài của chính user và chưa được user like trước đó. Cohort theo lịch sử trước request; C3 ưu tiên trước C1/C2. Các mẫu thiếu target được lưu trong JSON, không gán điểm 0.

| Nhóm | Ý nghĩa | Validation | Test |
|---|---|---:|---:|
| C0 | Chưa positive, không topic | 6 | 8 |
| C1 | Chưa positive, một topic đủ bài | 7 | 8 |
| C2 | Chưa positive, nhiều topic đủ bài | 7 | 6 |
| C3 | Chưa positive, topic thiếu bài | 0 | 0 |
| T1 | Một positive | 29 | 30 |
| T2 | 2–4 positives | 105 | 79 |
| W1 | 5–19 positives | 567 | 631 |
| W2 | Ít nhất 20 positives | 71 | 109 |

Gate yêu cầu >=30 users có target mỗi nhóm mỗi phase: **FAIL**. Đây là gate thiết kế dữ liệu, không phải lỗi engine. Không chạy model rồi chọn seed theo điểm.

## Điều đã học

Pilot ngắn không có W2; kéo lên 500 ticks khắc phục được nhưng cold trở nên hiếm. Uniform arrivals chưa đủ cho nghiên cứu tập trung onboarding. Catalog 500 bài cũng không còn scenario C3 dưới phân phối topic hiện tại. Không ép kết luận từ các nhóm nhỏ hoặc coi nhóm vắng là thành công.

## Action tiếp theo

1. Revision arrival trên pilot: thêm đợt user đến ngay trước cutoff, đủ mẫu cho C0/C1/C2 và T1; giữ phần bootstrap để quan sát warm. Đánh dấu đây là stress population có chủ đích, không đại diện tỷ lệ user thật.
2. Tách C3 thành scenario nội dung khan hiếm có config riêng thay vì bóp méo toàn bộ catalog chính; sửa protocol trước confirmatory run để gate có denominator đúng từng scenario.
3. Thêm onboarding informative/noisy, independent arrival audit và lưu config/hash; chạy lại pilot gate. Chỉ chuyển M3 khi đạt và formula parity hoàn tất.

Không có tuyên bố cải thiện relevance, production serving hoặc lợi ích cho người thật từ lượt này.

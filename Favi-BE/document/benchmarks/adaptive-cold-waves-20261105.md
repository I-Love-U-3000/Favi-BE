# Cold đã đủ mẫu trong pilot

Ngày 05/11/2026. Chạy OASIS native 1.000 users/500 ticks, seed 20261110,
arrival policy `cutoff-waves-v1`. 400 users bootstrap, 300 users tick 299,
300 users tick 399. Topic counts 0/1/3 cân bằng trong từng wave trước khi sinh
labels. Đây là stress population có chủ đích, không đại diện tỷ lệ arrivals thật.

Hoàn tất 102,437 giây: 500 posts, 11.427 likes, 1.412 follows, 2.303 comments.
Generator audit và read-back 28 files pass; 97 regression tests pass.
Raw: `.tools/datasets/adaptive-cold-waves-20261105-001`.
[JSON đầy đủ](adaptive-cold-waves-20261105.json) lưu denominator, excluded targets
và events hash. [Source inventory](adaptive-source-inventory-20261105.json)
lưu hash source/config chưa commit; không thay thế harness hashes trong bundle.

| Nhóm có target | Validation | Test | Kết quả |
|---|---:|---:|---|
| C0: cold/general | 93 | 96 | Đủ >=30 |
| C1: cold/1 topic | 94 | 94 | Đủ >=30 |
| C2: cold/nhiều topics | 92 | 97 | Đủ >=30 |
| C3: thiếu bài khớp topic | 0 | 0 | Chưa có scenario |
| T1: 1 positive | 7 | 50 | Validation thiếu |
| T2: 2–4 positives | 13 | 141 | Validation thiếu |
| W1: 5–19 positives | 321 | 398 | Đủ >=30 |
| W2: >=20 positives | 44 | 56 | Đủ >=30 |

So pilot uniform dài, cold tăng từ 6–8 lên 92–97 người có target mỗi nhóm.
Không xóa likes hoặc ép trạng thái: người mới có một tick hoạt động tự nhiên
trước cutoff. Các bài sinh sau cutoff bị loại khỏi targets hiện tại.

**Cold expansion đạt mục tiêu pilot; gate toàn bộ vẫn FAIL.** Wave quá sát
validation tạo nhiều cold nhưng ít T1/T2. Bước tiếp theo: giữ cold waves và
thêm một wave đến sớm hơn để tăng transition; C3 vẫn là scenario thiếu nội dung
riêng cần thiết kế trước confirmatory freeze. Không hạ ngưỡng để tuyên bố pass.

Topics hiện độc lập latent preferences: đủ thử dữ liệu, chưa chứng minh topic
feed hữu ích. Chưa có điểm model, HTTP mới hoặc kết luận relevance.

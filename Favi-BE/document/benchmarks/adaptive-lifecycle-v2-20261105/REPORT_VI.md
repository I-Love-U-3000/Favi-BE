# Revision tổng thể đã đủ các nhóm dữ liệu

Ngày 05/11/2026. **Milestone M2: gate dữ liệu pilot PASS.** Đây là dữ liệu
synthetic được thiết kế để thử đủ tình huống, không đại diện cơ cấu người thật.

## Đã làm và kiểm chứng

Hai lượt OASIS native, mỗi lượt 5.000 users, 2.000 posts, 500 ticks:

- Main lifecycle: 67.083 likes, 10.920 comments; 499,094 giây.
- Sparse-topic: 66.175 likes, 10.711 comments; 497,375 giây.

Hai lượt chạy đồng thời; thời gian trên là elapsed từng process, không phải
benchmark tốc độ độc lập. 98 regression tests pass. Analyzer đọc lại manifest,
kiểm tra arrival/preference policy và rare-topic cap; mỗi bundle có 31 files
được xác minh. Source và dữ liệu vẫn là pilot development, chưa freeze.

| Nhóm có target | Validation | Test |
|---|---:|---:|
| Cold không topic | 325 | 336 |
| Cold một topic | 341 | 328 |
| Cold nhiều topics | 334 | 332 |
| Transition: 1 positive | 314 | 368 |
| Transition: 2–4 positives | 356 | 975 |
| Warm: 5–19 positives | 1.324 | 2.051 |
| Warm: >=20 positives | 216 | 526 |
| Cold thiếu nội dung, scenario riêng | 670 | 685 |

Mọi ô bắt buộc đều >=30 users có target; không hạ gate và không chọn user theo
future feedback. [Báo cáo JSON](report.json) lưu tất cả users không target,
exclusions theo lý do, fingerprints và preference overlap. [Bảng máy sinh](review.md)
có trạng thái từng ô.

## Kết quả này nói lên điều gì

Đã giải quyết thiếu cold/transition/warm đồng thời trong candidate design đầu
tiên của revision. C3 được đo trong scenario có giới hạn chín bài music; catalog
main cân bằng không bị bóp méo để ép C3 xuất hiện.

Informative metadata khớp 100% initial top-interest slots theo định nghĩa của
generator; noisy và independent khoảng 25% và 24,3%. Noisy thay một trong một
hoặc hai trong ba slots, nên mức corruption thực tế khoảng 75%, không được gọi
là 50% thực đo. Các mode là alternative metadata trên cùng event log vì declared
topics không điều khiển exposure. C3 actual preferences được đổi sang rare topic;
alternative sidecars của sparse chỉ là provenance trước phép đổi đó.

## Còn thiếu trước kết luận nghiên cứu

Chưa chấm model, chưa có cải thiện relevance, chưa xác minh cold ranker C#/Python
hoặc formula parity mới, chưa có HTTP mới. Tags vẫn là simulator metadata, chưa
được chứng minh import PostTags production. Gate dữ liệu không tự chứng minh
thống kê hoặc tính đúng của toàn hệ thống.

Tiếp theo: chốt observed-tag contract/cold baseline và parity công thức; khóa
search grid, protocol, source và metric trước ba seed xác nhận. Sau đó mới chạy
đối chứng/router và phân tích theo các nhóm đã có. Không dùng điểm model để
điều chỉnh thiết kế dataset này.

## Tái lập

Config: `tools/oasis-smoke/configs/adaptive-lifecycle-v2.json`.
Generate bằng `generate_dataset.py --adaptive --arrival-policy lifecycle-v2`
với `--scenario main` hoặc `--scenario sparse-topic`, config trên và output mới.
Raw: `.tools/datasets/adaptive-lifecycle-{main|sparse}-20261105-001`.
Sau đó `summarize_adaptive_pilot.py --main <main> --sparse <sparse> --output <new-dir>`.
ZIP/receipts nằm trong `.tools/dataset-archives/`, phải kiểm tra receipt/checksum
trước khi chia sẻ hoặc tái sử dụng. Raw và archives không đưa vào Git.

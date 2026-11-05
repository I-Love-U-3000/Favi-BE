# Adaptive milestone: preflight 04/11/2026

## Kết quả thực thi

Branch: `codex/adaptive-recommender-v1`. Source nền đã commit: `276e085` (bao gồm milestone implementation `e824e79`). 91 tests Python pass trước checkpoint. Lượt này replay fixture native đã lưu bằng `benchmark_recommenders.py` và tạo ZIP mới bằng `package_dataset.py`; cả hai exit 0.

Fixture: 100 users, 31 posts, 858 events, 125 likes, 219 follows, 457 exposures; quality gate pass. Events SHA256: `7df76a183703caaa510b8089c9d627604189cf7b453cc7484fbe9e36099c970e`.

Raw replay: `.tools/benchmarks/adaptive-preflight-20261104-001`. Archive: `.tools/dataset-archives/adaptive-preflight-20261104-001.zip`; SHA256 `995396979ecc52e1999b39555a37e407336d0d7cd877aba6f152b876a8fdbff9`, 145672 bytes, 25 members. Mọi member được đọc lại và so checksum với source manifest. Đây là replay fixture cũ, chưa phải sinh lại native simulator.

| Phase | Users có target | Popularity nDCG@10 | Formula V1 offline nDCG@10 |
|---|---:|---:|---:|
| Validation | 23 | 0,188645 | 0,141518 |
| Test | 16 | 0,196170 | 0,184279 |

Số này chỉ chứng minh đường đọc dữ liệu, evaluate và đóng gói còn hoạt động. Fixture quá nhỏ để kết luận model nào tốt hơn. Chưa kiểm chứng C#/Python formula parity mới và chưa chạy HTTP ở lượt này.

## Phát hiện ảnh hưởng thiết kế

Generator cũ có 8 topics: food, transport, photography, technology, nature, art, sports, books. Catalog production `InterestTopics.All` có 10 topics: travel, food, sports, city, nature, technology, lifestyle, art, fitness, music. Dataset mới phải dùng trực tiếp catalog production; không tự suy diễn transport thành travel hoặc books thành art để tái sử dụng nhãn cũ.

`ProfileInterests` hiện lưu topics/version; read contract chỉ trả hai trường này. Benchmark cần event log có timestamp riêng để tái hiện sở thích tại cutoff; không giả định production đã có lịch sử preference đủ cho replay. Chưa có lý do sửa schema production chỉ để phục vụ simulator.

## Related work bước đầu và quyết định

| Công trình / nguồn primary | Nội dung đã xác minh | Áp dụng / khác biệt với Favi |
|---|---|---|
| [Rashid và cộng sự, 2002](https://experts.umn.edu/en/publications/getting-to-know-you-learning-new-user-preferences-in-recommender-/) | So sáu cách chọn item hỏi rating; có offline và thử nghiệm trực tiếp hơn 300 users | Favi chọn topic thay vì hỏi rating item. Phải đo giá trị onboarding riêng; không gọi topic giả lập là ý kiến người thật. Chưa tái hiện sáu kỹ thuật trong bài. |
| [Burke, 2002](https://experts.colorado.edu/display/pubid_285008) | Đã xác minh metadata bài hybrid, DOI và tác giả; trang trường không cung cấp nội dung phương pháp | Chỉ là đầu mối cho nghiên cứu hybrid. Cần đọc full text hợp lệ trước khi đối chiếu taxonomy/đối chứng; chưa tuyên bố router theo ngưỡng là mới. |
| [Ji và cộng sự](https://arxiv.org/abs/2010.11060) | Phân tích leakage khi không giữ global timeline, gồm future items và thay đổi thứ tự model | Giữ chronological split, item availability và thời điểm features; không đánh giá bài tương lai trong slate hiện tại. |
| [Schnabel và cộng sự, 2016](https://proceedings.mlr.press/v48/schnabel16.html) | Selection bias trong recommendation và hướng xử lý bằng causal inference | Log exposure policy và phân biệt replay với tác động triển khai. Không dùng IPS khi thiếu propensity hợp lệ. Chưa triển khai estimator của bài. |

Đây là bảng đọc nguồn ban đầu, không phải systematic literature review hoặc bằng chứng novelty.

## Việc tiếp theo

Tiếp tục tự động theo yêu cầu mới của người dùng, không đợi duyệt từng bước. Thứ tự: lưu inventory/checksum nguồn/config/locks; cụ thể hóa grid từ `expanded.json`; thiết kế event log onboarding/arrival và validator; chạy pilot có đủ cohort; kiểm chứng formula parity; chỉ freeze protocol khi các gate này đạt. Chưa chạy ba seed xác nhận khi pilot chưa hợp lệ.

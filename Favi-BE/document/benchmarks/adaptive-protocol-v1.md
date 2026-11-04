# Protocol adaptive-v1 — bản review

Ngày: 04/11/2026. Trạng thái: draft để review, chưa đóng băng và chưa chạy experiment. Đây là đầu ra đầu tiên của bước 1 trong [execution plan](../ADAPTIVE_RECOMMENDER_EXECUTION_PLAN.md).

## Câu hỏi và đối chứng

Kiểm tra liệu trì hoãn chuyển từ topic/general sang collaborative đến 5 positives có tốt hơn chuyển ngay sau positive đầu tiên. Đối chứng chính chuyển ở 1 positive; phương pháp đề xuất chọn ngưỡng trong `{1,5}` bằng validation. Cả hai chọn warm model từ EASE/PPR/LightGCN với cùng ngân sách. Ngưỡng 1 nằm trong tập ứng viên để phương pháp có thể chọn giữ nguyên baseline.

Popularity, Formula V1 hiện tại, Formula V2 và topic-only là đối chứng bổ sung. Phải kiểm chứng parity công thức C#/Python trước khi gọi kết quả offline là kết quả công thức Favi.

So thuật toán dùng likes-only. So hệ thống ghi rõ topics/follows/comments bổ sung. Production Like/Love/save được kiểm thử hành vi riêng; chưa coi benchmark likes-only đại diện đầy đủ các tín hiệu đó.

## Dữ liệu và thời gian

Dataset mới tên `favi-adaptive-v1-5k-seed-{seed}`, 5.000 users, 2.000 posts, 500 ticks. Ba seed xác nhận: 20261111, 20261112, 20261113; pilot seed 20261110. Dữ liệu cũ chỉ dùng thăm dò. Không thay seed do điểm thấp hoặc lỗi; lưu lỗi và sửa pipeline có version trước khi chạy lại toàn bộ.

Train lịch sử tick <300; validation yêu cầu feed tại tick 300, target ở [300,400); test yêu cầu feed tại tick 400, target ở [400,500). Refit cấu hình đã chọn bằng lịch sử <400 trước test. Chỉ item đã tồn tại và hợp lệ tại request mới nằm trong candidates hoặc target. Report số future-item targets bị loại. Timestamp ties không bị chia qua hai phía cutoff.

Features gồm lịch sử trước request, topic đã khai báo trước request và metadata item đã tồn tại. Không dùng latent interests hay future likes làm feature. Generator phải lưu user arrival, timestamp lựa chọn topic, tags và exposure policy; chưa giả định generator hiện tại đáp ứng những điều này.

Cohort phân theo trạng thái tại request: C0 không positive/không topic; C1 không positive/1 topic; C2 không positive/2–10 topics; C3 không positive/có topic nhưng hợp lệ ít hơn 10 bài khớp. C3 ưu tiên phân loại trước C1/C2 để các nhóm loại trừ nhau. T1 có 1 positive; T2 có 2–4; W1 có 5–19; W2 có ít nhất 20. Các nhóm có lịch sử vẫn giữ topic để phương pháp chuyển tiếp sử dụng.

Gate yêu cầu toàn bộ tham chiếu, ID, timestamp và topic hợp lệ, không future features, cohort inventory đầy đủ. Pilot phải có tối thiểu 30 users có target hợp lệ mỗi cohort tại mỗi cutoff; nếu thiếu thì điều chỉnh thiết kế trước khi đóng băng. Trên seed xác nhận, cohort dưới ngưỡng vẫn báo cáo nhưng không dùng để tuyên bố bảo toàn chất lượng cohort. User không có target là N/A, báo denominator và lý do riêng.

## Chọn cấu hình và đo

nDCG@10 macro theo user có target là metric chính. Báo riêng từng cohort và trung bình đều cohort như metric phụ, không đổi metric chính sau khi xem test. Recall@10, catalog coverage, feed rỗng, đa dạng tác giả, tuổi bài và chi phí là metric phụ. Các phương pháp dùng chung eligibility/candidate/target fingerprints và tie-break PostId tăng dần khi bằng điểm.

Mỗi warm model tối đa 8 cấu hình validation, dùng cùng grid cho baseline và router; grid cụ thể phải được lưu trước chạy. LightGCN dùng optimizer seeds 20261103/20261104/20261105, gộp trong từng dataset trước. Router chọn theo nDCG validation toàn tập; hòa trong 1e-12 ưu tiên ngưỡng 1, rồi thứ tự EASE/PPR/LightGCN. Không chọn theo test hoặc per-user oracle.

Tiêu chí giá trị thực dụng đề xuất: tăng ít nhất 0,001 nDCG tuyệt đối trung bình ba dataset, chênh lệch không âm trên ít nhất 2/3 dataset; không cohort đủ mẫu nào giảm quá 0,002 tuyệt đối trung bình. Đây là ngưỡng quyết định kỹ thuật để review, không phải ngưỡng ý nghĩa thống kê hoặc mức hài lòng đã được xác thực. Báo mọi chênh lệch, kể cả khi không đạt. Ba dataset chỉ hỗ trợ mô tả độ ổn định.

Sau test, bất kỳ sửa phương pháp dựa trên điểm test nào làm lượt đó thành exploratory; xác nhận tiếp cần seed holdout mới. Ablation bỏ topics/bỏ chuyển tiếp và robustness sở thích sai/exposure khác phải có config revision trước khi chạy; chưa thuộc lượt xác nhận chính này.

## Serving và giới hạn kết luận

Offline full catalog không tương đương serving top 300. Đo HTTP là bước sau, trên stack riêng và sample trong giới hạn artifact đã có. Ngân sách đề xuất để review: warm request p95 <=100 ms tại concurrency 10 với 1.000 requests sau 100 warm-up, không lỗi quyền truy cập; đo cold-process riêng tối thiểu 5 restart, không dùng 5 mẫu để tuyên bố p99. Ghi cấu hình máy, kích thước artifact, peak memory và thời gian rebuild. Mốc này là mục tiêu lab, chưa phải SLA production.

Simulator replay không chứng minh tăng engagement hoặc quan hệ nhân quả. MovieLens chỉ kiểm tra thêm interaction ranking, không đại diện onboarding Favi. Không tuyên bố novelty trước khi hoàn tất related work.

## Provenance và việc còn lại trước freeze

Commit nền quan sát: `c6880937995c3feb0179d14247ba2e8d4e4012cf`; working tree có nhiều thay đổi chưa commit. Commit này không đại diện đầy đủ source experiment.

Trước freeze còn bắt buộc: inventory/hash cả source chưa commit và configs/dependency locks; kiểm tra đọc lại archive; replay fixture nhỏ; parity công thức; bảng related work về preference elicitation, hybrid chuyển tiếp, temporal leakage và exposure bias; grid validation cụ thể; config generator/cohort pilot. Chưa hoàn tất những mục này thì chưa chạy batch xác nhận. Các ngưỡng đề xuất phía trên cần được review trước test.

Tài liệu nền: [cold feed](../COLD_START_FEED.md), [multiseed](../MULTISEED_RECOMMENDER_BENCHMARK.md), [external benchmark](../EXTERNAL_RECOMMENDER_BENCHMARK.md), [experimental serving](../EXPERIMENTAL_RECOMMENDER_FEED.md).

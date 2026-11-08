# Báo cáo tiến độ nghiên cứu Favi gửi giảng viên

Ngày 08/11/2026. Phạm vi: kết quả thực nghiệm đã hoàn thành trên dữ liệu mô phỏng,
đủ cho báo cáo tiến độ và trao đổi hướng khóa luận; chưa phải xác nhận sẵn sàng
bảo vệ hoặc triển khai thực tế.

## Kết quả chính

**Chưa đủ bằng chứng để thay phương án chuyển sang cá nhân hóa ngay sau lượt
thích đầu tiên bằng bộ chọn thời điểm chuyển phức tạp hơn.** Điểm chất lượng
trung bình tăng 0,000334784, thấp hơn ngưỡng lợi ích thực dụng 0,001 đã khóa trước
khi xem kết quả kiểm tra. Khi sở thích chủ đề kém tin cậy hoặc không có chủ đề,
phương án mới còn cho kết quả thấp hơn đối chứng. Đây là kết quả âm có giá trị
cho quyết định thiết kế, không phải lý do đổi ngưỡng hay chọn lại dữ liệu để tạo
kết quả dương.

## Câu hỏi và hướng nghiên cứu

Favi cần gợi ý cho cả người mới chưa có lịch sử, người chỉ có vài lượt thích và
người đã có đủ lịch sử. Người mới có thể chọn chủ đề quan tâm; nếu không chọn,
hệ thống dùng nội dung mặc định. Câu hỏi nghiên cứu là: **nên chuyển từ gợi ý theo
chủ đề/mặc định sang gợi ý cộng tác sau một hay năm tương tác tích cực, và lợi ích
chất lượng có đủ để biện minh cho chi phí phục vụ không?**

Đóng góp đang có là một quy trình so sánh xuyên suốt các mức lịch sử, có đối chứng,
kiểm tra độ nhạy, bằng chứng tái kiểm số học và kiểm chứng phục vụ thử nghiệm.
Không tuyên bố thuật toán mới chỉ vì kết hợp EASE, PPR hoặc LightGCN. Điểm cần
thảo luận với giảng viên là mức đóng góp của câu hỏi, thiết kế thực nghiệm và
phân tích điều kiện thất bại trong phạm vi khóa luận.

## Thiết kế thực nghiệm

- Sáu bộ dữ liệu: ba seed độc lập cho kịch bản chủ đề thông thường và ba seed
  tương ứng cho kịch bản chủ đề khan hiếm. Mỗi bộ có 5.000 người dùng, tối đa
  2.000 bài và 500 bước thời gian. Đây là dữ liệu tổng hợp, chưa hiệu chỉnh theo
  hành vi người thật. Không gộp thành 30.000 người dùng thật độc lập.
- Tách theo thời gian: học trước bước 300; chọn cấu hình bằng các lượt thích từ
  bước 300 đến trước 400; huấn luyện lại bằng lịch sử trước 400; đánh giá từ bước
  400 đến trước 500. Không chọn cấu hình hoặc ngưỡng bằng tập kiểm tra.
- Đối chứng B1 chọn mô hình/cấu hình tại ngưỡng một lượt thích. Phương án R chọn
  chung mô hình/cấu hình và ngưỡng một hoặc năm lượt thích từ cùng ngân sách tìm
  kiếm. Vì vậy chênh lệch R−B1 không luôn chỉ phản ánh ngưỡng chuyển.
- So sánh thêm Popularity, topic-only, công thức Favi V1/V2 và từng họ EASE,
  PPR, LightGCN. Các phương án có cùng điều kiện bài hợp lệ và tập đích; công
  thức Favi còn nhận follows/comments, còn mô hình cộng tác học từ likes. Do đó
  đây là so sánh các hệ thống đã khai báo tín hiệu, không phải đầu vào tuyệt đối
  giống nhau cho mọi thuật toán.
- Chỉ số chính nDCG@10 đánh giá các bài được thích trong tập giữ lại có xuất
  hiện ở vị trí cao trong 10 gợi ý hay không. Điểm này không phải phần trăm hài
  lòng. Người không có đích đánh giá được báo riêng, không gán điểm 0.
- Trung bình theo ba bộ dữ liệu; các lần lặp tối ưu của LightGCN được gộp trong
  từng bộ trước. Không coi chúng là những cộng đồng độc lập bổ sung. Chưa đưa
  ra kết luận có ý nghĩa thống kê hay tác động nhân quả.

## Kết quả chất lượng

Các số dưới đây là trung bình nDCG@10 của ba seed, làm tròn chín chữ số thập phân.
“Chủ đề khan hiếm” là kịch bản kiểm tra riêng, không được trộn vào chỉ tiêu chính.

| Phương án | Chủ đề thông thường | Chủ đề khan hiếm |
|---|---:|---:|
| Popularity | 0.003498267 | 0.003616857 |
| Chỉ theo chủ đề | 0.011028109 | 0.004555654 |
| Công thức Favi V1 | 0.005501391 | 0.004244103 |
| Công thức Favi V2 | 0.005817985 | 0.004711142 |
| EASE | 0.012447980 | 0.011231291 |
| PPR | 0.010293460 | 0.007949836 |
| LightGCN | 0.013670212 | 0.010676852 |
| B1: chuyển từ lượt thích đầu tiên | 0.012925747 | 0.010601171 |
| R: chọn chung ngưỡng và mô hình | 0.013260531 | 0.010601171 |

Các hàng EASE/PPR/LightGCN dùng ngưỡng một; cấu hình được chọn trên validation.
Việc LightGCN có điểm test trung bình cao nhất trong bảng không cho phép chọn lại
nó sau khi xem test rồi gọi là kết luận xác nhận mới.

Ở kịch bản chính, R tăng điểm trên hai trong ba seed nhưng mức tăng trung bình
không đạt ngưỡng thực dụng đã định. Nhóm một lượt thích và nhóm hai đến bốn lượt
thích tăng lần lượt 0.001508894 và 0.001114060; không dùng hai nhóm này thay cho
kết quả tổng thể. Các nhóm chưa có lượt thích có R=B1 vì dùng cùng fallback.
Ở kịch bản khan hiếm, R=B1 trên cả ba seed.

Kiểm tra giữ cùng cấu hình và chỉ thay ngưỡng cho mức tăng trung bình 0.000416166,
nhưng vẫn có seed âm. Khi replay với chủ đề nhiễu, chủ đề độc lập hoặc bỏ chủ đề,
R−B1 lần lượt là −0.001044392, −0.001499580 và −0.001437935. Replay giữ nguyên
log hành vi và lựa chọn mô hình; chưa đo cách người thật thay đổi hành vi.

## Khả năng phục vụ và chi phí

Endpoint trực tiếp trong phòng thử nghiệm đạt p95 57.2082 ms cho B1 và 57.4861 ms
cho R, dưới mốc 100 ms. Tuy nhiên, thử tải qua gateway thất bại vì gần như mọi
request bị giới hạn tốc độ và trả HTTP 429. Không dùng thời gian trả 429 làm
thời gian gợi ý thành công. Artifact phục vụ thực tế chỉ có mẫu 250 người dùng,
chưa phải toàn bộ 5.000 người.

Sáu lượt ranking gốc mất tổng cộng 12.641,235283 giây, khoảng 3 giờ 30 phút
41 giây, gồm cả kiểm gate, tìm kiếm, huấn luyện lại, đánh giá và ghi dữ liệu.
Có 33 lần huấn luyện lại duy nhất với tổng 199,635498 giây; không cộng trùng
theo nhãn phương án. Thời gian tìm kiếm riêng và RAM cực đại khi huấn luyện gốc
chưa được đo. Vì vậy chưa kết luận phương án nào có hiệu quả chi phí tổng thể tốt hơn.

## Độ tin cậy và giới hạn

Dữ liệu và lựa chọn đã khóa có hashes, manifests và archives; đã khôi phục bản
sao để tính lại kết quả từ dự đoán lưu trữ. Kiểm toán độc lập đối chiếu lựa chọn
validation với protocol, định danh người/bài và các điều kiện import. Mốc hiện tại
sửa hai lỗi của công cụ kiểm chứng: nhận đúng manifest native projection và giữ
các kiểm source/checksum/count/FK hoạt động khi bật Python optimization. Các sửa
này không thay dữ liệu, cấu hình đã chọn hoặc điểm nghiên cứu lịch sử.

Giới hạn cần giữ khi trình bày: dữ liệu mô phỏng; chỉ ba seed cho mỗi kịch bản;
likes quan sát không phải nhãn sở thích đầy đủ; chưa có user study; offline
likes-only khác live Like/Love và save; chưa đủ serving toàn tập, gateway, training
RAM hoặc tái dựng toàn bộ stack trên máy sạch. Các PR chưa merge và chưa deploy.

## Đề xuất trao đổi với giảng viên

Giữ kết luận “chưa đạt lợi ích thực dụng” và trình bày điều kiện ngưỡng chuyển
thất bại như một kết quả nghiên cứu. Xin ý kiến về việc lấy quy trình đánh giá
xuyên mức lịch sử và phân tích độ nhạy chủ đề làm trọng tâm; nếu cần bổ sung,
ưu tiên dữ liệu công khai hoặc đánh giá người dùng theo protocol mới được duyệt
trước, thay vì chỉnh lại thí nghiệm đã xem test. Chưa thực hiện các thí nghiệm
bổ sung này trong báo cáo hiện tại.

Nguồn kiểm tra: [protocol khóa](adaptive-protocol-v1.md),
[tổng hợp nghiên cứu](adaptive-thesis-evidence-20261108/REPORT_VI.md),
[bảng sinh từ JSON](adaptive-thesis-evidence-20261108/TABLES_VI.md),
[khả năng tái kiểm và phản biện](adaptive-defense-readiness-20261108/REPORT_VI.md),
[chi phí](adaptive-cost-audit-20261108/REPORT_VI.md),
[HTTP](adaptive-http-20261108/REPORT_VI.md),
[kiểm runtime](adaptive-validation-runtime-20261108/REPORT_VI.md).

# `Stress` + `Spike` + `Soak` Test Scenarios (No code)

## A) Stress tests (vượt ngưỡng thiết kế)

### STRESS-AUTH-LOGIN-MASS_USERS
- **Mô phỏng**: số lượng login đồng thời vượt mức load bình thường.
- **Mục tiêu**: tìm điểm gãy (breaking point), hành vi khi cạn tài nguyên.

### STRESS-FEED-REFRESH-CONTINUOUS_MASS
- **Mô phỏng**: nhiều user refresh feed liên tục, khoảng nghỉ rất ngắn.
- **Mục tiêu**: phát hiện nghẽn DB/read pool, queue backlog.

### STRESS-REACTION-HOTSPOT-MASS_USERS
- **Mô phỏng**: hàng loạt user cùng like/unlike/comment vào 1 post hot.
- **Mục tiêu**: lock contention, lost update, counter inconsistency.

### STRESS-NOTI-STORM-HOT_ENTITY
- **Mô phỏng**: tương tác dồn vào một creator/post trong thời gian rất ngắn.
- **Mục tiêu**: xác minh notification pipeline không bị bùng nổ không kiểm soát.

### STRESS-WRITE-PATH-MASS_CREATE
- **Mô phỏng**: nhiều user đồng thời create post + comment + reaction.
- **Mục tiêu**: ổn định transaction, side-effect notification/vector.

### STRESS-MEDIA-PIPELINE-PARTIAL_FAILURES
- **Mô phỏng**: tăng mạnh create post/media đồng thời với tỷ lệ request lỗi định dạng/timeout xử lý phụ.
- **Mục tiêu**: kiểm tra khả năng chịu lỗi cục bộ, tránh hỏng dữ liệu dây chuyền.

---

## B) Spike tests (tăng tải đột ngột)

### SPIKE-FEED-TRAFFIC-SUDDEN_JUMP
- **Mô phỏng**: traffic feed tăng vọt trong 10–30s rồi hạ.
- **Mục tiêu**: autoscale logic (nếu có), khả năng hồi phục nhanh.

### SPIKE-LOGIN-AFTER_EVENT
- **Mô phỏng**: nhiều user login cùng lúc sau event (livestream, push notification).
- **Mục tiêu**: auth burst handling.

### SPIKE-TRENDING_POST-VIRAL
- **Mô phỏng**: post influencer bất ngờ viral, read/comment/like tăng đột ngột.
- **Mục tiêu**: chịu tải điểm nóng.

---

## C) Soak tests (ổn định dài hạn)

### SOAK-FEED-REACTION-MIX-LONG_RUN
- **Thời lượng**: nhiều giờ.
- **Mô phỏng**: workload gần thật (read nhiều hơn write).
- **Mục tiêu**: memory leak, connection leak, hiệu năng suy giảm theo thời gian.

### SOAK-REFRESH-PATTERN-LONG_RUN
- **Mô phỏng**: user refresh theo chu kỳ, không quá dồn dập nhưng kéo dài.
- **Mục tiêu**: kiểm tra drift latency, tăng error rate chậm.

### SOAK-CONSISTENCY-LONG_RUN
- **Mô phỏng**: định kỳ create/like/comment và read lại để verify data.
- **Mục tiêu**: phát hiện lệch dữ liệu tích lũy.

---

## D) Tiêu chí dừng test khẩn cấp
- Error rate vượt ngưỡng an toàn liên tục.
- Tăng 5xx kéo dài.
- DB không phục hồi sau khoảng cooldown.
- Data integrity bị phá (counter âm, duplicate vượt kiểm soát).
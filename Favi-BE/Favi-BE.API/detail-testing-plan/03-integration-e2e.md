# `Integration` + `E2E` Test Scenarios (No code)

## 1) E2E user journey tiêu chuẩn

### E2E-NEW_USER-FIRST_DAY_FLOW
- **Luồng**: đăng ký/login -> follow vài account -> mở feed -> like/comment/share -> logout.
- **Mục tiêu**: xác nhận các module kết nối end-to-end.

### E2E-CREATOR-PUBLISH-TO-AUDIENCE
- **Luồng**: influencer tạo post -> followers refresh feed -> người dùng tương tác.
- **Mục tiêu**: feed propagation + engagement pipeline.

---

## 2) Cross-module consistency

### INT-POST_REACTION_FEED_CONSISTENCY
- Like tại post detail, kiểm tra feed card có phản ánh đúng.

### INT-COMMENT_NOTIFICATION_CONSISTENCY
- User A comment post của User B -> B nhận notification -> mở detail thấy comment.

### INT-SHARE_PROFILE_FEED_CONSISTENCY
- Share tại detail -> xuất hiện tại `feed-with-reposts` + profile shares.

### INT-SEARCH_TO_POSTDETAIL
- Search -> mở detail -> thao tác reaction/comment -> quay lại search list.

### INT-FOLLOW_TO_FEED_PROPAGATION
- User A follow User B -> refresh feed/profile list của A.
- Kiểm tra nội dung từ B xuất hiện/không xuất hiện đúng theo privacy + policy hiện tại.

### INT-PROFILE_PRIVACY-CROSS_ENDPOINTS
- Đổi privacy profile/post rồi kiểm tra lại qua profile detail, feed card, search result.
- Kỳ vọng không có endpoint nào lộ dữ liệu trái policy.

---

## 3) Read-after-write / eventual consistency

### INT-RAW-CREATE_POST_THEN_READ
- Tạo post xong đọc lại ngay ở detail/feed/profile.
- Xác định SLA nhất quán chấp nhận được.

### INT-RAW-REACT_THEN_READ_COUNT
- Bắn reaction rồi đọc lại nhiều endpoint đếm tương tác.

### INT-COUNTER-INTEGRITY-CROSS_ENDPOINTS
- Đối chiếu `like/comment/share count` giữa feed card, post detail, profile list.
- Không chấp nhận lệch kéo dài ngoài SLA eventual consistency đã định.

### INT-FOLLOW-COUNTER-INTEGRITY
- Thực hiện follow/unfollow liên tiếp có retry nhẹ.
- Đối chiếu follower/following count giữa profile owner và profile viewer.
- Không chấp nhận lệch kéo dài ngoài SLA eventual consistency đã định.

### INT-ASYNC-SIDE_EFFECTS
- Các xử lý async (vector index/NSFW/noti) không làm hỏng luồng chính.

### INT-RANKING-FRESHNESS-BOUNDARY
- So sánh feed trước và sau mốc rerank (ví dụ 15 phút).
- Kỳ vọng thứ hạng thay đổi hợp lý, không stale quá lâu hoặc nhảy bất thường.

---

## 4) Regression-focused integration set
- Tập nhỏ cố định chạy sau mỗi thay đổi quan trọng:
  1. login -> feed
  2. create post -> detail -> feed
  3. like/unlike loop
  4. comment parent/child
  5. share + feed-with-reposts
  6. semantic + related fallback
# `Functional` Test Scenarios (No code)

## A. Auth & Session

### FUNC-AUTH-LOGIN-VALID
- Login đúng credential, nhận token đúng format, expiry hợp lệ.

### FUNC-AUTH-LOGIN-INVALID
- Sai password, sai email, account banned/inactive.
- Kiểm tra thông báo lỗi đúng chuẩn, không lộ thông tin nhạy cảm.

### FUNC-AUTH-MULTI_DEVICE_SESSION
- 1 user login nhiều thiết bị.
- Kiểm tra policy session (cho phép hay overwrite).

### FUNC-AUTH-PRIVACY-LEAKAGE_GUARD
- User không đủ quyền không được đọc private post/profile qua feed/search/related/direct endpoint.
- Có block/mute thì kết quả đọc phải tuân thủ policy.

### FUNC-AUTH-PROFILE-PRIVACY_MATRIX
- Kiểm tra ma trận quyền đọc profile theo trạng thái `public/followers/private`.
- So sánh 3 actor: owner, follower hợp lệ, non-follower.
- Kỳ vọng response/status nhất quán giữa profile endpoint và feed/search entry points.

---

## B. Feed

### FUNC-FEED-PAGINATION
- Page 1/2/3 trả dữ liệu đúng, không duplicate liên trang.

### FUNC-FEED-PAGINATION_STABILITY_UNDER_NEW_POSTS
- Trong lúc user đang scroll, có post mới được tạo.
- Kiểm tra không mất item, không duplicate giữa page N và N+1.

### FUNC-FEED-DEDUPE_POST_REPOST
- Cùng một nội dung xuất hiện qua post/repost path.
- Kiểm tra dedupe đúng rule.

### FUNC-FEED-REFRESH_CONSISTENCY
- Refresh nhiều lần trong thời gian ngắn.
- Kiểm tra dữ liệu không “nhảy loạn” ngoài kỳ vọng ranking.

### FUNC-FEED-EMPTY_STATE
- User mới ít follow: có fallback feed hợp lệ.

---

## C. Post/Media

### FUNC-POST-CREATE_WITH_VALID_MEDIA
- Tạo post có media hợp lệ từ pool freeze.

### FUNC-POST-CREATE_INVALID_MEDIA
- Sai định dạng/kích thước/tệp lỗi.
- Kiểm tra validate + mã lỗi rõ ràng.

### FUNC-POST-UPDATE_ARCHIVE_DELETE
- Update caption/privacy -> archive -> delete logic.
- Kiểm tra ảnh hưởng lên feed/profile.

### FUNC-POST-DELETE_CASCADE_SOFTDELETE
- Xóa/ẩn post phải xử lý đúng reaction/comment/share/notification liên quan theo rule hiện tại.
- Post đã xóa/archive không được xuất hiện sai ở feed/profile/detail.

### FUNC-MEDIA-PARTIAL_FAILURE_HANDLING
- Tạo post/media thành công DB nhưng fail ở bước xử lý phụ (thumbnail/vector/async side-effect).
- Xác minh trạng thái cuối cùng rõ ràng: rollback hoặc compensation theo thiết kế.

---

## D. Reaction/Comment/Share

### FUNC-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER
- Like/unlike lặp vòng.
- Xác minh idempotency và trạng thái cuối.

### FUNC-REACTION-DUPLICATE_GUARD
- Gửi like trùng nhanh liên tiếp.
- Kỳ vọng không tạo row duplicate.

### FUNC-WRITE-IDEMPOTENCY-ALL_ACTIONS
- Áp dụng cho `follow/unfollow`, `share/unshare`, `save/unsave`, `delete/restore`.
- Double-click/retry không làm sai trạng thái cuối hoặc tạo duplicate record.

### FUNC-FOLLOW-GRAPH-INTEGRITY
- Follow/unfollow với cùng cặp user dưới retry/race nhẹ.
- Kỳ vọng: không self-follow, không duplicate edge, follower/following count không lệch.
- Trường hợp account private: yêu cầu follow phải đi đúng flow policy hiện tại.

### FUNC-COMMENT-PARENT_CHILD_DEPTH_LIMIT
- Tạo parent + child, chặn depth > 2.

### FUNC-COMMENT-WITH_URL_CONTENT
- Comment chứa URL hợp lệ; verify parser/render.

### FUNC-SHARE-UNIQUE_RULE
- 1 user không share trùng cùng post nếu rule unique bật.

---

## E. Search/Related

### FUNC-SEARCH-SEMANTIC_BASIC
- Query phổ biến và query hiếm.

### FUNC-RELATED-TAG_BASED_FALLBACK
- Bài không đủ vector score -> fallback theo tag.

---

## F. Notification Side-effects

### FUNC-NOTI-REACTION_COMMENT_FOLLOW
- Tạo action -> notification tạo đúng loại, đúng receiver, không spam duplicate bất thường.
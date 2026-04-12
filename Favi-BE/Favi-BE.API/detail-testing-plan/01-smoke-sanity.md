# `Smoke` + `Sanity` Test Scenarios (No code)

## 1) Smoke — kiểm tra hệ thống còn sống

### SMOKE-AUTH-LOGIN-SINGLE_USER
- **Mục tiêu**: xác nhận login hoạt động.
- **Tác nhân**: 1 user.
- **Luồng**: login -> nhận token -> gọi endpoint profile/feed.
- **Pass**: 2xx, token hợp lệ, response shape đúng.

### SMOKE-FEED-READ-SINGLE_USER
- **Mục tiêu**: xác nhận feed endpoint có dữ liệu.
- **Tác nhân**: 1 user.
- **Luồng**: gọi `feed`, `guest-feed`, `latest`, `profile feed`.
- **Pass**: không rỗng bất thường, pagination hoạt động.

### SMOKE-POST-CREATE_DELETE-SINGLE_USER
- **Mục tiêu**: xác nhận vòng đời post cơ bản.
- **Luồng**: create post -> read detail -> delete/archive -> read lại.
- **Pass**: trạng thái đổi đúng, không 5xx.

### SMOKE-REACTION-LIKE-SINGLE_USER
- **Mục tiêu**: xác nhận like hoạt động.
- **Luồng**: like một post đã seed -> verify count/state.
- **Pass**: count tăng đúng, trạng thái user reacted đúng.

### SMOKE-COMMENT-CREATE-SINGLE_USER
- **Mục tiêu**: comment tạo được.
- **Luồng**: tạo comment parent -> đọc lại.
- **Pass**: comment hiển thị đúng post, đúng user.

### SMOKE-PROFILE-PRIVACY-READ-SINGLE_USER
- **Mục tiêu**: xác nhận profile endpoint tôn trọng privacy cơ bản.
- **Luồng**: đọc profile public -> đọc profile private/followers-only bằng user không đủ quyền.
- **Pass**: profile public đọc được; profile bị chặn trả đúng status/shape theo policy.

---

## 2) Sanity — kiểm tra nhanh sau thay đổi nhỏ

### SANITY-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER
- **Mục tiêu**: đảm bảo toggle ổn định.
- **Luồng**: like -> unlike -> like -> unlike (10–50 vòng).
- **Quan sát**:
  - count không âm, không lệch.
  - không duplicate reaction.
  - trạng thái cuối phản ánh thao tác cuối.
- **Pass**: không race-condition ở mức single user.

### SANITY-AUTH-LOGIN_LOGOUT_RELOGIN-SINGLE_USER
- **Luồng**: login -> logout -> login lại -> gọi endpoint bảo vệ.
- **Pass**: token cũ không dùng được (nếu có revoke policy), token mới dùng được.

### SANITY-FEED-REFRESH-REPEAT-SINGLE_USER
- **Mục tiêu**: refresh liên tục không lỗi.
- **Luồng**: refresh feed 30–100 lần liên tiếp.
- **Pass**: response ổn định, không tăng lỗi dần.

### SANITY-SEARCH-RELATED-FALLBACK-SINGLE_USER
- **Luồng**: gọi semantic search -> gọi related post.
- **Pass**: có fallback hợp lệ khi semantic yếu.

### SANITY-SHARE-UNSHARE-SINGLE_USER
- **Luồng**: share post -> kiểm tra feed/profile shares -> unshare/hide (nếu có).
- **Pass**: trạng thái share nhất quán giữa các endpoint.
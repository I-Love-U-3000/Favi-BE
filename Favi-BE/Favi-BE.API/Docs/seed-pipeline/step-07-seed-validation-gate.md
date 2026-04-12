# Step 7 — Validation Gate (Global)

## Mục tiêu
Thực thi validation tổng hợp sau các step seed, đảm bảo toàn bộ dataset hợp lệ trước khi export/finalize.

## Điều kiện để chạy
- Đã hoàn thành các step dữ liệu trước đó (ít nhất Step 1–5, Step 6 nếu có).
- Có thể truy vấn đủ bảng liên quan trong DB.

## Input
- `seed/SeedValidator.cs`
- Dữ liệu đã seed trong DB (`Profiles`, `EmailAccounts`, `Follows`, `Posts`, `PostMedias`, `Reactions`, `Comments`, `Reposts`, `Tags`, `PostTags`, `Notifications`).

## Output
- PASS/FAIL toàn cục của pipeline.
- Log cảnh báo (không fail) cho các rule dạng warning.

## Must-have checks
- Users:
  - count trong range
  - username unique
  - email unique
  - `avatar_url` không rỗng
  - `cover_url` không rỗng
  - có đủ role `Admin`, `Moderator`, `User`
- Follows:
  - count trong range (theo feasible max)
  - không self-follow
  - không duplicate edge
  - FK hợp lệ
- Posts/Media:
  - posts count trong range
  - 100% posts có media
  - media URL không rỗng
  - FK media hợp lệ
- Engagement:
  - reactions/comments/reposts count trong range
  - reaction không duplicate theo target pair `(post|comment|repost, profile_id)`
  - mỗi reaction chỉ có đúng 1 target (`post_id` hoặc `comment_id` hoặc `repost_id` hoặc `collection_id`)
  - comment không orphan
  - comment depth tối đa 2 (không có reply của reply)
  - có reply comments
  - có comments chứa URL
  - có reactions cho comments
  - repost không duplicate theo `(profile_id, original_post_id)`
- Tags/PostTags:
  - tags count trong range
  - tag name unique
  - không duplicate `(post_id, tag_id)`
  - FK `PostTags` hợp lệ
  - post không có tag => `WARNING` (không fail)
- Notifications (nếu có):
  - actor/recipient hợp lệ, không trùng nhau
  - FK target post/comment hợp lệ nếu có

## Behavior
- Bất kỳ lỗi bắt buộc nào => `FAIL` và dừng pipeline.
- Chỉ các rule warning mới được phép tiếp tục.

## Definition of Done
- `SeedValidator` pass toàn bộ checks bắt buộc.
- Pipeline đủ điều kiện qua Step 8 export.

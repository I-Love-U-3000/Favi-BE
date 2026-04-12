# Step 6 — Seed Lightweight Notifications

## Mục tiêu
Seed notifications mức nhẹ để giữ side-effects hợp lệ cho các flow follow/reaction/comment/share mà không làm nhiễu benchmark core.

## Điều kiện để chạy
- Đã có dữ liệu từ Step 2 và Step 4:
  - `Follows`
  - `Reactions`
  - `Comments`
  - `Reposts`
- Đã có `Posts` để map owner làm recipient.

## Input
- `SeedContext`
- `Posts`, `Follows`, `Reactions`, `Comments`, `Reposts`

## Output
- DB: `Notifications`
- File export (optional nhưng đã bật): `seed-output/notifications.csv`

## Rules bắt buộc
- Seed nhẹ, có cap tổng số notifications.
- Sinh notification từ các side-effects:
  - `Follow` -> `NotificationType.Follow`
  - `Reaction` -> `NotificationType.Like`
  - `Comment` -> `NotificationType.Comment`
  - `Repost` -> `NotificationType.Share`
- Không tạo notification self-action (`actor == recipient`).

## Validation cục bộ (ngay sau step)
- `actor == recipient` => `FAIL`
- `TargetPostId` không hợp lệ => `FAIL`
- `TargetCommentId` không hợp lệ => `FAIL`
- `Message` rỗng => `FAIL`

## Validation global (Step 7)
- Actor/Recipient phải tồn tại và khác nhau.
- FK `TargetPostId`/`TargetCommentId` hợp lệ nếu có.

## Metadata/log cần ghi
- `created_notifications`
- `export_path`
- breakdown theo type (`Follow/Like/Comment/Share`)

## Definition of Done
- Notifications đã được seed nhẹ, pass local validation, pass global validator ở Step 7.

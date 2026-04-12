# Step 4 — Seed Engagement

## Mục tiêu
Tạo dữ liệu `Reactions`, `Comments`, `Reposts` có skew hợp lý để benchmark ranking/feed consistency.

## Điều kiện để chạy
- Đã có profiles từ Step 1.
- Đã có posts từ Step 3.
- Có cấu hình ranges cho reactions/comments/reposts.

## Input
- `SeedContext`
- `SeedConfig.Reactions`
- `SeedConfig.Comments`
- `SeedConfig.Reposts`
- Danh sách profiles + posts

## Output
- DB:
  - `Reactions`
  - `Comments`
  - `Reposts`
- File export:
  - `seed-output/reactions.csv`
  - `seed-output/comments.csv`
  - `seed-output/reposts.csv`

## Rules bắt buộc
- Reactions multi-target (post/comment/repost) nhưng mỗi reaction chỉ target đúng 1 entity.
- Reactions unique theo từng target pair:
  - post: `(post_id, profile_id)`
  - comment: `(comment_id, profile_id)`
  - repost: `(repost_id, profile_id)`
- Comments depth chỉ `1–2` (parent + reply), không orphan.
- Có comment chứa URL (content có `http://` hoặc `https://`).
- Có reaction cho comment (không chỉ reaction cho post).
- Reposts unique theo `(profile_id, original_post_id)`.
- Phân phối engagement lệch (hot/cold), không đều.

## Phân phối gợi ý (deterministic, skewed)
- Post popularity theo heavy-tail (Zipf-like): hot posts nhận phần lớn engagement.
- Reaction target mix:
  - Post ~72%
  - Comment ~20%
  - Repost ~8%
- Comment tree:
  - Root comments chiếm đa số
  - Reply rate khoảng 25–35% trên mỗi post có root comments
  - Không sinh reply của reply
- Actor activity mix theo role hành vi:
  - power > casual > lurker

## Validation cục bộ (ngay sau step)
- Duplicate reaction theo target pair => `FAIL`.
- Reaction có nhiều target hoặc không có target => `FAIL`.
- Orphan comment => `FAIL`.
- Comment depth > 2 => `FAIL`.
- Không có reply comment => `FAIL`.
- Không có comment chứa URL => `FAIL`.
- Không có reaction cho comment => `FAIL`.
- Duplicate repost pair => `FAIL`.
- FK invalid => `FAIL`.

## Metadata/log cần ghi
- `created_reactions`
- `created_comments`
- `created_reposts`
- `hot_post_share`
- `export_paths`

## Definition of Done
- Step 4 pass validation cục bộ và sẵn sàng vào Step 7 global gate.

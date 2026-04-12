# Step 5 — Seed Tags & PostTags

## Mục tiêu
Tạo tập `Tags` và `PostTags` deterministic để phục vụ phân loại nội dung, related-by-tag và benchmark path theo tag.

## Điều kiện để chạy
- Đã có `Posts` từ Step 3.
- Có `SeedConfig.Tags` hợp lệ (`50–120`).

## Input
- `seed/config.cs`
- Danh sách `Posts` hiện có trong DB
- `SeedContext` (deterministic random)

## Output
- DB:
  - `Tags`
  - `PostTags`
- Files:
  - `seed-output/tags.csv`
  - `seed-output/post-tags.csv`

## Rules bắt buộc
- Tổng tags trong range `50–120`.
- Mỗi post gắn `1–3` tags.
- `Tag.Name` unique.
- Phân phối popularity lệch (Zipf/heavy-tail), không đều.

## Validation cục bộ (ngay sau step)
- Duplicate tag name => `FAIL`
- Duplicate `(post_id, tag_id)` => `FAIL`
- FK invalid (`PostId`/`TagId`) => `FAIL`
- Post không có tag => `WARNING` (không fail, đúng spec)

## Metadata/log cần ghi
- `created_tags`
- `created_post_tags`
- `distinct_posts_tagged`
- `top_tags_by_usage`
- `export_paths`

## Definition of Done
- `Tags`/`PostTags` được seed và export thành công.
- Pass toàn bộ check bắt buộc của step.
- Sẵn sàng cho Step 7 (global validation gate).

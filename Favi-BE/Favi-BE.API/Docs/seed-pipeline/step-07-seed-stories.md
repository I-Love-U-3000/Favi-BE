# Step 7 — Seed Stories

## Mục tiêu
Tạo dữ liệu `Stories` active cho benchmark read path và k6 smoke tests.

## Điều kiện để chạy
- Đã có profiles từ Step 1.
- Đã có `run-image-set.json` từ Step 3 (SeedPosts) — bắt buộc, không tự tạo.
- Có cấu hình `SeedConfig.Stories`.

## Input
- `SeedContext`
- `SeedConfig.Stories` (range: 500–2000)
- `SeedConfig.UserRoleDistribution` (lurker/casual/power)
- Danh sách profiles hiện có
- `run-image-set.json` (đọc lại từ seed-output, không upload runtime)

## Output
- DB: `Stories`
- File export:
  - `seed-output/stories.csv` (columns: story_id, profile_id, media_url, thumbnail_url, privacy, created_at, expires_at, is_archived, is_nsfw)

## Rules bắt buộc
- Không upload ảnh runtime — dùng URLs từ `run-image-set.json`.
- Tất cả stories được seed phải active: `ExpiresAt = CreatedAt + 24h`, `CreatedAt` trong vòng 23h qua.
- `IsArchived = false`, `IsNSFW = false` cho tất cả seeded stories.
- Mọi randomness phải đi qua `SeedContext.Random`.
- Privacy phân phối: 80% Public, 15% Followers, 5% Private.

## Phân phối theo role
- **power** (5% users): 2–4 stories mỗi người.
- **casual** (25% users): 40% chance có 1 story.
- **lurker** (70% users): 0 stories.

Role được suy từ username index (`user_{idx}`) và `SeedConfig.UserRoleDistribution` — không lưu role vào DB.

## Image pick
- Index-based deterministic: `runImageSet[(profileIndex * 7 + storyIndex) % runImageSet.Count]`
- `ThumbnailUrl = MediaUrl` (dùng cùng URL, không tạo thumbnail riêng)

## Validation cục bộ (ngay sau bước tạo, trước DB insert)
- Tổng stories trong range `[SeedConfig.Stories.Min, SeedConfig.Stories.Max]` → `FAIL` nếu ngoài range.
- `ProfileId` hợp lệ (FK tồn tại trong profiles) → `FAIL`.
- `MediaUrl` không rỗng → `FAIL`.
- `ExpiresAt > CreatedAt` → `FAIL`.
- `ExpiresAt > UtcNow` (tất cả stories phải còn active) → `FAIL`.

## Metadata/log cần ghi
- `created_stories`
- `export_path`

## Definition of Done
- Step 7 pass validation cục bộ, `stories.csv` đã export, pipeline tiếp tục Step 8 (Global Validation Gate).

# Step 9 — Export Dataset

> **Note:** File giữ tên `step-08-seed-export-dataset.md` vì lý do backward compat, nhưng trong pipeline thực tế đây là **Step 9** (Step 7 = Seed Stories, Step 8 = Validation Gate).

## Mục tiêu
Khóa output dataset benchmark sau khi Step 8 pass, tạo manifest runtime để chứng minh repeatability.

## Điều kiện để chạy
- Step 8 đã PASS (global validation).
- Các artifacts core đã có trên `seed-output/`.

## Input
- `seed/SeedExport.cs`
- DB đã seed và validate
- Folder `seed-output/`

## Output
- Runtime manifest: `seed-output/seed-manifest.json`
- Danh sách artifacts core đã xác nhận tồn tại:
  - `users.csv`
  - `tokens.csv` (fresh runtime token bootstrap)
  - `follows.csv`
  - `posts.csv`
  - `post-medias.csv`
  - `reactions.csv`
  - `comments.csv`
  - `reposts.csv`
  - `tags.csv`
  - `post-tags.csv`
  - `stories.csv`
  - `image-catalog.json`
  - `run-image-set.json`
- Optional artifacts (nếu có):
  - `notifications.csv`
  - `story-views.csv`

## Rules bắt buộc
- Thiếu artifact core => `FAIL` ngay.
- Manifest phải ghi:
  - `seedKey`
  - `generatedAt`
  - counts runtime (`users/posts/follows/reactions/comments/reposts/tags/notifications/stories`)
  - `artifacts`
  - `optionalArtifacts` (`notifications.csv`, `story-views.csv` nếu có)

## Validation cục bộ
- Kiểm tra tồn tại toàn bộ core files trước khi ghi manifest.
- Ghi manifest thành công mới coi Step 8 pass.

## Definition of Done
- Dataset export đã freeze và có `seed-manifest.json` runtime để dùng lại cho before/after benchmark.

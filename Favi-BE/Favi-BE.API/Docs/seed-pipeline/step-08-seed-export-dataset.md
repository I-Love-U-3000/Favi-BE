# Step 8 — Export Dataset

## Mục tiêu
Khóa output dataset benchmark sau khi Step 7 pass, tạo manifest runtime để chứng minh repeatability.

## Điều kiện để chạy
- Step 7 đã PASS (global validation).
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
  - `image-catalog.json`
  - `run-image-set.json`

## Rules bắt buộc
- Thiếu artifact core => `FAIL` ngay.
- Manifest phải ghi:
  - `seedKey`
  - `generatedAt`
  - counts runtime (`users/posts/follows/reactions/comments/reposts/tags/notifications`)
  - `artifacts`
  - `optionalArtifacts` (`notifications.csv` nếu có)

## Validation cục bộ
- Kiểm tra tồn tại toàn bộ core files trước khi ghi manifest.
- Ghi manifest thành công mới coi Step 8 pass.

## Definition of Done
- Dataset export đã freeze và có `seed-manifest.json` runtime để dùng lại cho before/after benchmark.

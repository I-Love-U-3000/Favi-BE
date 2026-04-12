# Step 3 — Seed Posts + Media

## Mục tiêu
Tạo dữ liệu `Posts` và `PostMedias` deterministic cho benchmark feed/read path.

## Điều kiện để chạy
- Đã có users/profiles từ Step 1.
- Có cấu hình `SeedConfig.Posts`.
- Có `run-image-set.json` (nếu chưa có thì tạo deterministic từ `image-catalog.json`).

## Input
- `SeedContext`
- `SeedConfig.Posts`
- Danh sách profiles hiện có
- Image source policy từ Step 0

## Output
- DB: `Posts`, `PostMedias`
- File export:
  - `seed-output/posts.csv`
  - `seed-output/post-medias.csv`

## Rules bắt buộc
- Số lượng post trong range `10,000–12,000`.
- Mỗi post phải có đúng `1` media (baseline).
- `PostMedias.Url` lấy từ `run-image-set.json`.
- Không upload ảnh runtime trong seed benchmark.

## Validation cục bộ (ngay sau step)
- Count posts/media đúng expected.
- `100%` posts có media.
- Media URL không null/rỗng.
- FK `PostMedias.PostId` hợp lệ.

## Metadata/log cần ghi
- `created_posts`
- `created_post_medias`
- `run_image_set_size`
- `export_paths`

## Definition of Done
- Step 3 pass validation cục bộ và sẵn sàng cho Step 4.

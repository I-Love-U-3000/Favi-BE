# Step 0 — Seed Constants & Context

## Mục tiêu
Thiết lập nguồn cấu hình duy nhất cho toàn bộ pipeline để đảm bảo deterministic và repeatable.

## Điều kiện để chạy
- Có `seed/config.cs`, `seed/SeedContext.cs`, `seed/seed-manifest.json`.

## Input
- `SEED_KEY`
- count ranges cho users/posts/follows/reactions/comments/reposts/tags/vectorized posts
- role distribution
- output paths
- image policy (`image-catalog`, `run-image-set`)

## Output
- `SeedConfig` sẵn sàng cho toàn pipeline
- `SeedContext` với stable random seed
- manifest template để bước sau ghi runtime manifest

## Rules bắt buộc
- Không dùng `GetHashCode()` cho deterministic seed.
- Mọi randomness phải đi qua `SeedContext`.
- Config ranges phải hợp lệ (`min <= max`).

## Validation cục bộ
- `SEED_KEY` không rỗng
- Các ranges hợp lệ
- Output path config đầy đủ

## Definition of Done
- Tất cả step 1..9 dùng chung config/context, không còn random trôi nổi.

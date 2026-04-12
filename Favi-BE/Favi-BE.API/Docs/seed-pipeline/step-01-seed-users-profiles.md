# Step 1 — Seed Users / Profiles

## Mục tiêu
Tạo tập user nền deterministic để làm source-of-truth cho toàn bộ các step sau.

## Input
- `SeedContext` (`SeedKey`, `Random` stable hash)
- `SeedConfig.Users` và `SeedConfig.UserRoleDistribution`

## Output
- DB: `Profiles` + `EmailAccounts`
- File: `seed-output/users.csv`

## Schema export đề xuất
- `profile_id`
- `username`
- `display_name`
- `email`
- `password`
- `role`
- `activity_role`
- `avatar_url`
- `cover_url`
- `privacy_level`
- `follow_privacy_level`
- `is_banned`
- `created_at`
- `last_active_at`

## Rule bắt buộc
- Tổng user: `5000`
- `username` unique
- `email` unique
- Password seed cố định (ví dụ `123456`) và hash bằng BCrypt trước khi lưu DB
- Deterministic theo `SeedKey`
- Timestamp rải trong khoảng 30–90 ngày
- `avatar_url` và `cover_url` bắt buộc có giá trị (không null/rỗng)
- Role account phải có đủ `Admin`, `Moderator`, `User` (không seed 1 role duy nhất)

## Phân phối role
- `lurker`: 70%
- `casual`: 25%
- `power`: 5%

## Role account (quyền hệ thống)
- 1 account `Admin`
- một tập nhỏ account `Moderator` (ưu tiên trong nhóm active)
- phần lớn account là `User`

## Validation gate (fail là dừng)
- Duplicate email => FAIL
- Duplicate username => FAIL
- Count khác expected => FAIL
- `avatar_url` null/rỗng => FAIL
- `cover_url` null/rỗng => FAIL
- Thiếu một trong các role `Admin|Moderator|User` => FAIL

## Ghi chú thực thi
- Không dùng `string.GetHashCode()` để tạo seed.
- Mọi random đều đi qua `SeedContext.Random`.
- Step 1 hoàn tất mới chạy Step 2.

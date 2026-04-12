# Step 9 — Optional Snapshot

## Mục tiêu
Tạo snapshot DB sau khi export để phục hồi nhanh môi trường benchmark.

## Điều kiện để chạy
- Step 8 đã PASS.
- Dataset đã freeze và manifest runtime đã được ghi.

## Input
- DB state hiện tại sau pipeline
- Thông tin `seedKey` và `generatedAt` từ manifest

## Output
- Optional: `seed.sql` (ví dụ từ `pg_dump`)

## Rules
- Snapshot phản ánh đúng dataset vừa export.
- Không thay đổi dữ liệu seed trong quá trình dump.

## Validation
- Snapshot file tồn tại và có kích thước hợp lệ.
- Có thể restore trên môi trường test sạch.

## Definition of Done
- Có snapshot dùng để restore nhanh cho debug/rerun benchmark.

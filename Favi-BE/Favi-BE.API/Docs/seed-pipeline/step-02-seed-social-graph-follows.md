# Step 2 — Seed Social Graph (Follows)

## Mục tiêu
Sinh social graph có skew thực tế, deterministic và an toàn dữ liệu để benchmark feed fan-out-on-read.

## Input
- User list từ Step 1
- `SeedContext`
- `SeedConfig.Follows`

## Output
- DB: `Follows`
- File: `seed-output/follows.csv`

## Schema export
- `follower_id`
- `followee_id`
- `created_at`

## Rule bắt buộc
- Không self-follow
- Không duplicate edge
- Tổng edge trong range `50,000–70,000`
- Mỗi user follow trong khoảng `0–20`
- Average xấp xỉ `~12`

## Mô hình phân phối
Áp dụng skewed model, không seed đều:
- Có thể dùng `Zipf` hoặc `90-9-1` để tạo heavy-tail
- Khi chọn `followee`, ưu tiên account đã có nhiều follower (preferential attachment)

Mục tiêu là tạo đồ thị gần hành vi thực tế:
- Đa số user có ít follower
- Một nhóm nhỏ có follower cao rõ rệt

## Validation gate (fail là dừng)
- Có self-follow => FAIL
- Có duplicate edge => FAIL
- Graph rỗng => FAIL
- FK không hợp lệ => FAIL
- Count ngoài range => FAIL

## Metrics nên log
- Total follows
- Avg/min/max out-degree
- p50/p90/p99 follower count
- Top users by follower count

## Ghi chú thực thi
- Step 2 chỉ xử lý social graph; chưa đụng posts/reactions.
- Re-run cùng `SeedKey` phải cho kết quả deterministic.

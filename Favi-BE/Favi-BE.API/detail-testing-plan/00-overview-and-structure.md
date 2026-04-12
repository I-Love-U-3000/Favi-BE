# Detail Testing Plan — Structure by `Loại Test`

## Nguyên tắc tổ chức (bắt buộc)
Folder này chỉ chia theo **1 trục duy nhất: loại test**.

- Không chia lẫn theo feature/module.
- Không tạo file kiểu “login-tests”, “feed-tests”, “trending-tests” riêng lẻ.
- Mọi module (`auth`, `feed`, `post`, `reaction`, `comment`, `share`, `search`) sẽ nằm trong từng loại test dưới dạng scenario matrix.

## Mục tiêu
- Theo dõi dễ: biết test nào thuộc functional, test nào thuộc load/stress.
- So sánh trước/sau tối ưu dễ hơn.
- Bao phủ đủ hành vi chuẩn mạng xã hội: đơn user, đa user, hotspot, refresh liên tục.

## Danh sách file
1. `01-smoke-sanity.md`
2. `02-functional.md`
3. `03-integration-e2e.md`
4. `04-load.md`
5. `05-stress-spike-soak.md`
6. `06-test-data-execution-matrix.md`

## `Top 12 Must-Run` (không được thiếu)
1. Idempotency + race condition cho write actions
2. Read-after-write consistency
3. Counter integrity (`like/comment/share count`)
4. Pagination stability khi dữ liệu thay đổi liên tục
5. Authorization + privacy leakage
6. Delete cascade / soft-delete correctness
7. Hotspot contention trên post viral
8. Feed ranking freshness boundary (theo chu kỳ rerank)
9. Fan-out on read worst-case (power user)
10. Notification storm control
11. Search/related fallback correctness
12. Media pipeline partial-failure correctness

## Quy ước đặt tên scenario
`[TEST_TYPE]-[MODULE]-[BEHAVIOR]-[SCALE]`

Ví dụ:
- `FUNC-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER`
- `LOAD-AUTH-LOGIN-BULK_USERS`
- `STRESS-FEED-REFRESH_HOTSPOT-MASS_USERS`

## Template mô tả mỗi scenario (no code)
- **Mục tiêu**
- **Tiền điều kiện** (seed/data/token)
- **Tác nhân** (bao nhiêu user/VU)
- **Luồng thao tác** (step-by-step)
- **Biến thể** (normal / edge / failure)
- **KPI cần theo dõi** (latency, error rate, throughput)
- **Pass/Fail**
- **Dấu hiệu lỗi phổ biến**

## Kết nối với seed plan
- Chỉ dùng dataset seed deterministic đã freeze.
- Không tạo data random mới trong lúc benchmark.
- Test read baseline phải tách khỏi test write/media/vector để tránh nhiễu.
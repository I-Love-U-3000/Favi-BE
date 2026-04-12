# Test Data & Execution Matrix (No code)

## 1) Data policy
- Dùng dataset đã seed deterministic theo `SEED_KEY`.
- Dùng `tokens.csv` đã bootstrap, tránh login random mỗi iteration nếu không test auth.
- Dùng `run-image-set.json` freeze cho media scenarios.
- Không tạo user/post random ngoài seed khi chạy benchmark so sánh before/after.

## 2) Nhóm người dùng mô phỏng
- **Lurker**: chủ yếu read/refresh.
- **Casual**: read + like/comment nhẹ.
- **Power**: create/share/comment thường xuyên.
- **Influencer**: ít nhưng tạo hotspot tự nhiên.

## 3) Execution matrix theo loại test

### Smoke/Sanity
- Quy mô: nhỏ
- Mục tiêu: xác nhận sống, phát hiện lỗi rõ ràng
- Tần suất: trước mọi phiên test lớn

### Functional
- Quy mô: nhỏ-vừa
- Mục tiêu: đúng logic nghiệp vụ
- Tần suất: khi đổi logic endpoint/model

### Integration/E2E
- Quy mô: vừa
- Mục tiêu: đúng luồng liên module
- Tần suất: trước khi chốt baseline

### Load
- Quy mô: vừa-lớn
- Mục tiêu: đo baseline hiệu năng
- Tần suất: before/after mỗi cụm tối ưu

### Stress/Spike/Soak
- Quy mô: lớn hoặc kéo dài
- Mục tiêu: điểm gãy, hồi phục, ổn định dài hạn
- Tần suất: milestone

## 4) Bộ `Top 12 Must-Run` bắt buộc
1. `FUNC-WRITE-IDEMPOTENCY-ALL_ACTIONS` (idempotency + race condition)
2. `INT-RAW-CREATE_POST_THEN_READ` (read-after-write)
3. `INT-COUNTER-INTEGRITY-CROSS_ENDPOINTS` (counter integrity)
4. `FUNC-FEED-PAGINATION_STABILITY_UNDER_NEW_POSTS` (pagination stability)
5. `FUNC-AUTH-PRIVACY-LEAKAGE_GUARD` (auth/privacy leakage)
6. `FUNC-POST-DELETE_CASCADE_SOFTDELETE` (delete cascade/soft-delete)
7. `STRESS-REACTION-HOTSPOT-MASS_USERS` (hotspot contention)
8. `INT-RANKING-FRESHNESS-BOUNDARY` (ranking freshness boundary)
9. `LOAD-FEED-FANOUT_WORSTCASE-POWER_USERS` (fan-out worst-case)
10. `STRESS-NOTI-STORM-HOT_ENTITY` (notification storm control)
11. `LOAD-SEARCH_RELATED-FALLBACK_CORRECTNESS` (search/related fallback)
12. `STRESS-MEDIA-PIPELINE-PARTIAL_FAILURES` (media partial failures)

### 4.1) Mapping nhanh theo file
- `01-smoke-sanity.md`: pre-flight (`SMOKE-*`, `SANITY-*`)
- `02-functional.md`: #1, #4, #5, #6
- `03-integration-e2e.md`: #2, #3, #8
- `04-load.md`: #9, #11
- `05-stress-spike-soak.md`: #7, #10, #12

## 4.2) Coverage bổ sung theo seed entities
- `Follow`:
  - `SANITY-FOLLOW-UNFOLLOW-LOOP-SINGLE_USER`
  - `FUNC-FOLLOW-GRAPH-INTEGRITY`
  - `INT-FOLLOW_TO_FEED_PROPAGATION`
  - `INT-FOLLOW-COUNTER-INTEGRITY`
- `Profile` (privacy/policy):
  - `SMOKE-PROFILE-PRIVACY-READ-SINGLE_USER`
  - `FUNC-AUTH-PROFILE-PRIVACY_MATRIX`
  - `INT-PROFILE_PRIVACY-CROSS_ENDPOINTS`

## 5) Checklist cho mỗi lần chạy
- Cùng env/resource (`docker-compose.resource-allocation`)
- Cùng seed key + dataset + script
- Warm-up trước baseline
- Lưu đủ metrics + logs + test metadata
- Ghi rõ thay đổi duy nhất giữa before/after

## 6) Báo cáo kết quả (khuyến nghị)
- Tóm tắt p50/p95/p99 theo từng loại test
- Top endpoint chậm nhất
- Tỷ lệ lỗi theo endpoint
- Kết luận: nhanh/chậm hơn ở đâu, vì sao, có đánh đổi gì
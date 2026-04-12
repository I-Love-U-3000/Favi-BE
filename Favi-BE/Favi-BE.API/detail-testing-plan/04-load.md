# `Load` Test Scenarios (No code)

## Mục tiêu
Đo hiệu năng ở tải cao nhưng còn trong vùng kỳ vọng vận hành bình thường.

## KPI chính
- P50/P95/P99 latency
- Error rate
- Throughput (req/s)
- Tỷ lệ timeout
- DB pressure và connection pool health

## 1) LOAD-AUTH-LOGIN-BULK_USERS
- **Mô phỏng**: nhiều user login đồng thời.
- **Biến thể**:
  - login một lần rồi reuse token
  - login định kỳ theo batch
- **Quan sát**: auth bottleneck, rate limit, lock contention.

## 2) LOAD-FEED-REFRESH-STEADY_USERS
- **Mô phỏng**: nhiều user refresh feed đều đặn.
- **Pattern**: mỗi user 2–5 giây refresh 1 lần trong phiên dài.
- **Quan sát**: p95 feed, pagination consistency, DB read spikes.

## 2.1) LOAD-FEED-FANOUT_WORSTCASE-POWER_USERS
- **Mô phỏng**: nhóm user có follow graph dày (follow/follower lớn) đọc feed liên tục.
- **Mục tiêu**: bóc tách worst-case fan-out on read, phát hiện query plan suy giảm.
- **Quan sát**: p95/p99 riêng nhóm power users.

## 3) LOAD-REACTION-STEADY_MIX
- **Mô phỏng**: tỷ lệ 70% read, 20% reaction/comment, 10% post create.
- **Quan sát**: cân bằng read/write, deadlock/timeout.

## 4) LOAD-LIKE_UNLIKE-ROTATING_USERS
- **Mô phỏng**: nhiều user thay phiên like/unlike trên tập post hot.
- **Mục tiêu**: kiểm tra idempotency + lock row hot.

## 5) LOAD-TRENDING_POST-HEAVY_READ
- **Mô phỏng**: 1 post trending của influencer bị đọc liên tục + tương tác vừa phải.
- **Quan sát**: post detail cacheability, reaction count consistency.

## 6) LOAD-PROFILE_SCROLL-STEADY
- **Mô phỏng**: user vào profile influencer, scroll nhiều page.
- **Quan sát**: paging sâu, query plan ổn định.

## 7) LOAD-SEARCH_RELATED-MODERATE
- **Mô phỏng**: semantic search + related ở mức vừa.
- **Quan sát**: tránh làm nhiễu baseline feed, đo riêng endpoint search.
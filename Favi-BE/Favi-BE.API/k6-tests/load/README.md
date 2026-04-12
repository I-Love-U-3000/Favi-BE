# Load Test Suite

K6 load scenarios triển khai theo `detail-testing-plan/04-load.md`.

## Scenarios

1. `scenario-1-auth-login-bulk-users.js` - `LOAD-AUTH-LOGIN-BULK_USERS`
2. `scenario-2-feed-refresh-steady-users.js` - `LOAD-FEED-REFRESH-STEADY_USERS`
3. `scenario-2-1-feed-fanout-worstcase-power-users.js` - `LOAD-FEED-FANOUT_WORSTCASE-POWER_USERS`
4. `scenario-3-reaction-steady-mix.js` - `LOAD-REACTION-STEADY_MIX`
5. `scenario-4-like-unlike-rotating-users.js` - `LOAD-LIKE_UNLIKE-ROTATING_USERS`
6. `scenario-5-trending-post-heavy-read.js` - `LOAD-TRENDING_POST-HEAVY_READ`
7. `scenario-6-profile-scroll-steady.js` - `LOAD-PROFILE_SCROLL-STEADY`
8. `scenario-7-search-related-moderate.js` - `LOAD-SEARCH_RELATED-MODERATE`

## Chạy toàn bộ

```powershell
pwsh ./k6-tests/load/run-all.ps1 -BaseUrl https://localhost:7138 -ShowSummary
```

## Chạy từng scenario

```powershell
k6 run --env BASE_URL=https://localhost:7138 --env SEED_OUTPUT_DIR=../../seed-output k6-tests/load/scenario-2-feed-refresh-steady-users.js
```

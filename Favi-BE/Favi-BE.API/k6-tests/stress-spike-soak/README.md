# Stress + Spike + Soak k6 Suite

Triển khai theo `detail-testing-plan/05-stress-spike-soak.md`.

## A) Stress
- `scenario-a1-stress-auth-login-mass-users.js`
- `scenario-a2-stress-feed-refresh-continuous-mass.js`
- `scenario-a3-stress-reaction-hotspot-mass-users.js`
- `scenario-a4-stress-noti-storm-hot-entity.js`
- `scenario-a5-stress-write-path-mass-create.js`
- `scenario-a6-stress-media-pipeline-partial-failures.js`

## B) Spike
- `scenario-b1-spike-feed-traffic-sudden-jump.js`
- `scenario-b2-spike-login-after-event.js`
- `scenario-b3-spike-trending-post-viral.js`

## C) Soak
- `scenario-c1-soak-feed-reaction-mix-long-run.js`
- `scenario-c2-soak-refresh-pattern-long-run.js`
- `scenario-c3-soak-consistency-long-run.js`

## Chạy toàn bộ
```powershell
pwsh ./k6-tests/stress-spike-soak/run-all.ps1 -BaseUrl https://localhost:7138 -ShowSummary
```

## Chạy riêng soak lâu hơn
```powershell
pwsh ./k6-tests/stress-spike-soak/run-all.ps1 -SoakDuration 2h
```

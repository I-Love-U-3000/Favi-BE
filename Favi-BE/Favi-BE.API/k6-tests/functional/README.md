# Functional k6 Suite

Triển khai theo `detail-testing-plan/02-functional.md`.

## Files
- `scenario-1-auth-session-privacy.js`
- `scenario-2-feed-functional.js`
- `scenario-3-post-media-functional.js`
- `scenario-4-reaction-comment-share-functional.js`
- `scenario-5-search-related-functional.js`
- `scenario-6-notification-sideeffects-functional.js`

## Run all
```powershell
pwsh ./k6-tests/functional/run-all.ps1 -BaseUrl https://localhost:7138 -ShowSummary
```

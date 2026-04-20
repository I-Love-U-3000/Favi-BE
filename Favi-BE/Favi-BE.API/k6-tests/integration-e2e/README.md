# Integration + E2E k6 Suite

Triển khai theo `detail-testing-plan/03-integration-e2e.md`.

## Files
- `scenario-1-e2e-user-journey.js`
- `scenario-2-cross-module-consistency.js`
- `scenario-3-read-after-write-consistency.js`
- `scenario-4-regression-focused-set.js`

## Run all
```powershell
pwsh ./k6-tests/integration-e2e/run-all.ps1 -BaseUrl https://localhost:7138 -ShowSummary
```

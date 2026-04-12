#!/usr/bin/env pwsh

param(
    [string]$BaseUrl = "https://localhost:7138",
    [string]$SeedDir = "../../seed-output",
    [string]$K6Bin = "k6",
    [string]$SoakDuration = "30m",
    [switch]$ShowSummary
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Running STRESS/SPIKE/SOAK Test Suite" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "Seed Output: $SeedDir"
Write-Host "Soak Duration: $SoakDuration"
Write-Host "K6 Binary: $K6Bin"
Write-Host ""

$resultsDir = "./test-results/stress-spike-soak"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$scenarios = @(
    "scenario-a1-stress-auth-login-mass-users.js",
    "scenario-a2-stress-feed-refresh-continuous-mass.js",
    "scenario-a3-stress-reaction-hotspot-mass-users.js",
    "scenario-a4-stress-noti-storm-hot-entity.js",
    "scenario-a5-stress-write-path-mass-create.js",
    "scenario-a6-stress-media-pipeline-partial-failures.js",
    "scenario-b1-spike-feed-traffic-sudden-jump.js",
    "scenario-b2-spike-login-after-event.js",
    "scenario-b3-spike-trending-post-viral.js",
    "scenario-c1-soak-feed-reaction-mix-long-run.js",
    "scenario-c2-soak-refresh-pattern-long-run.js",
    "scenario-c3-soak-consistency-long-run.js"
)

$passed = 0
$failed = 0
$results = @()

foreach ($scenario in $scenarios) {
    Write-Host ""
    Write-Host "Running: $scenario" -ForegroundColor Yellow
    Write-Host "---"

    $scenarioName = $scenario -replace '.js$', ''
    $resultFile = "$resultsDir/$scenarioName-result.json"

    $output = & $K6Bin run `
        --env BASE_URL=$BaseUrl `
        --env SEED_OUTPUT_DIR=$SeedDir `
        --env SOAK_DURATION=$SoakDuration `
        --out json=$resultFile `
        "k6-tests/stress-spike-soak/$scenario" 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ PASSED: $scenario" -ForegroundColor Green
        $passed++
        $results += @{ scenario = $scenario; status = "PASSED"; code = 0 }
    }
    else {
        Write-Host "✗ FAILED: $scenario" -ForegroundColor Red
        $failed++
        $results += @{ scenario = $scenario; status = "FAILED"; code = $LASTEXITCODE }
    }

    if ($output) {
        Write-Host $output
    }
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Passed: $passed" -ForegroundColor $(if ($passed -eq 0) { "Red" } else { "Green" })
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "Total:  $($passed + $failed)"
Write-Host "Results saved to: $resultsDir"
Write-Host ""

if ($ShowSummary) {
    Write-Host "Detailed Results:" -ForegroundColor Yellow
    foreach ($result in $results) {
        $icon = if ($result.status -eq "PASSED") { "✓" } else { "✗" }
        $color = if ($result.status -eq "PASSED") { "Green" } else { "Red" }
        Write-Host "$icon $($result.scenario): $($result.status)" -ForegroundColor $color
    }
    Write-Host ""
}

if ($failed -eq 0) {
    Write-Host "✓ All stress/spike/soak tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ Some tests failed. Please review the results." -ForegroundColor Red
    exit 1
}

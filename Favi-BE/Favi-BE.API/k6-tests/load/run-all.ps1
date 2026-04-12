#!/usr/bin/env pwsh

param(
    [string]$BaseUrl = "https://localhost:7138",
    [string]$SeedDir = "../../seed-output",
    [string]$K6Bin = "k6",
    [switch]$ShowSummary
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Running LOAD Test Suite" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "Seed Output: $SeedDir"
Write-Host "K6 Binary: $K6Bin"
Write-Host ""

$resultsDir = "./test-results/load"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$scenarios = @(
    "scenario-1-auth-login-bulk-users.js",
    "scenario-2-feed-refresh-steady-users.js",
    "scenario-2-1-feed-fanout-worstcase-power-users.js",
    "scenario-3-reaction-steady-mix.js",
    "scenario-4-like-unlike-rotating-users.js",
    "scenario-5-trending-post-heavy-read.js",
    "scenario-6-profile-scroll-steady.js",
    "scenario-7-search-related-moderate.js"
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
        --out json=$resultFile `
        "k6-tests/load/$scenario" 2>&1

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
    Write-Host "✓ All load tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ Some tests failed. Please review the results." -ForegroundColor Red
    exit 1
}

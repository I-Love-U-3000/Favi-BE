#!/usr/bin/env pwsh

param(
    [string]$BaseUrl = "https://localhost:7138",
    [string]$SeedDir = "../../seed-output",
    [string]$K6Bin = "k6",
    [switch]$ShowSummary
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Running FUNCTIONAL Test Suite" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "Seed Output: $SeedDir"
Write-Host "K6 Binary: $K6Bin"
Write-Host ""

$resultsDir = "./test-results/functional"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$scenarios = @(
    "scenario-1-auth-session-privacy.js",
    "scenario-2-feed-functional.js",
    "scenario-3-post-media-functional.js",
    "scenario-4-reaction-comment-share-functional.js",
    "scenario-5-search-related-functional.js",
    "scenario-6-notification-sideeffects-functional.js"
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
        "k6-tests/functional/$scenario" 2>&1

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

    if ($output) { Write-Host $output }
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

if ($failed -eq 0) { exit 0 } else { exit 1 }

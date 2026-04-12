#!/usr/bin/env pwsh

# SMOKE Test Runner Script (PowerShell version)
# For Windows users

param(
    [string]$BaseUrl = "https://localhost:7138",
    [string]$K6Bin = "k6",
    [switch]$ShowSummary
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Running SMOKE Test Suite" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "K6 Binary: $K6Bin"
Write-Host ""

# Create results directory
$resultsDir = "./test-results/smoke"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

# Array of test scenarios
$scenarios = @(
    "smoke-auth-login.js",
    "smoke-feed-read.js",
    "smoke-post-create-delete.js",
    "smoke-reaction-like.js",
    "smoke-comment-create.js",
    "smoke-profile-privacy-read.js"
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

    $env:BASE_URL = $BaseUrl

    $output = & $K6Bin run `
        --env BASE_URL=$BaseUrl `
        --out json=$resultFile `
        "k6-tests/smoke/$scenario" 2>&1

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
    Write-Host "✓ All smoke tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ Some tests failed. Please review the results." -ForegroundColor Red
    exit 1
}
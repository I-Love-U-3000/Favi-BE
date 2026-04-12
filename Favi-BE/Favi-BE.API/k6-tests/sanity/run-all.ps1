#!/usr/bin/env pwsh

# SANITY Test Runner Script (PowerShell version)
# For Windows users

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$SeedDir = "../../seed-output",
    [string]$K6Bin = "k6",
    [switch]$ShowSummary
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Running SANITY Test Suite" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "Seed Output: $SeedDir"
Write-Host "K6 Binary: $K6Bin"
Write-Host ""

# Create results directory
$resultsDir = "./test-results/sanity"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

# Array of test scenarios
$scenarios = @(
    "scenario-1-reaction-like-unlike-loop.js",
    "scenario-2-auth-login-logout-relogin.js",
    "scenario-3-feed-refresh-repeat.js",
    "scenario-4-search-related-fallback.js",
    "scenario-5-share-unshare.js"
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
    $env:SEED_OUTPUT_DIR = $SeedDir
    
    $output = & $K6Bin run `
        --env BASE_URL=$BaseUrl `
        --env SEED_OUTPUT_DIR=$SeedDir `
        --out json=$resultFile `
        "k6-tests/sanity/$scenario" 2>&1
    
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
    Write-Host "✓ All sanity tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ Some tests failed. Please review the results." -ForegroundColor Red
    exit 1
}

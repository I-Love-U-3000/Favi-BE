#!/bin/bash

# SANITY Test Runner Script
# Chạy tất cả các sanity test scenario

set -e

# Configuration
BASE_URL="${BASE_URL:-http://localhost:5000}"
SEED_OUTPUT_DIR="${SEED_OUTPUT_DIR:-../../seed-output}"
K6_BIN="${K6_BIN:-k6}"
RESULTS_DIR="./test-results/sanity"

echo "================================"
echo "Running SANITY Test Suite"
echo "================================"
echo "Base URL: $BASE_URL"
echo "Seed Output: $SEED_OUTPUT_DIR"
echo "K6 Binary: $K6_BIN"
echo ""

# Create results directory
mkdir -p "$RESULTS_DIR"

# Array of test scenarios
declare -a scenarios=(
    "scenario-1-reaction-like-unlike-loop.js"
    "scenario-2-auth-login-logout-relogin.js"
    "scenario-3-feed-refresh-repeat.js"
    "scenario-4-search-related-fallback.js"
    "scenario-5-share-unshare.js"
)

# Run each scenario
passed=0
failed=0

for scenario in "${scenarios[@]}"; do
    echo ""
    echo "Running: $scenario"
    echo "---"
    
    scenario_name="${scenario%.js}"
    result_file="$RESULTS_DIR/${scenario_name}-result.json"
    
    if $K6_BIN run \
        --env BASE_URL="$BASE_URL" \
        --env SEED_OUTPUT_DIR="$SEED_OUTPUT_DIR" \
        --out json="$result_file" \
        "k6-tests/sanity/$scenario"; then
        echo "✓ PASSED: $scenario"
        ((passed++))
    else
        echo "✗ FAILED: $scenario"
        ((failed++))
    fi
done

echo ""
echo "================================"
echo "Test Summary"
echo "================================"
echo "Passed: $passed"
echo "Failed: $failed"
echo "Total:  $((passed + failed))"
echo "Results saved to: $RESULTS_DIR"
echo ""

if [ $failed -eq 0 ]; then
    echo "✓ All sanity tests passed!"
    exit 0
else
    echo "✗ Some tests failed. Please review the results."
    exit 1
fi

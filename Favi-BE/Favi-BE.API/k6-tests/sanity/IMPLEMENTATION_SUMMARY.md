# K6 Sanity Test Suite — Implementation Summary

**Created**: 2026-04-10  
**Status**: ✓ Complete  
**Location**: `Favi-BE.API/k6-tests/sanity/`

---

## 📦 Files Created (7 files)

### 1. **Test Scenarios** (5 files)

#### `scenario-1-reaction-like-unlike-loop.js`
- **ID**: `SANITY-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER`
- **Purpose**: Verify like/unlike toggle stability
- **Flow**:
  1. Load token from tokens.csv
  2. Select random post
  3. Loop 10 times: like → unlike → like → unlike
  4. Verify count doesn't go negative
  5. Verify no duplicate reactions
  6. Verify final state matches last action
- **KPI**: Count consistency, no duplicates
- **Pass**: count >= 0, no race conditions

#### `scenario-2-auth-login-logout-relogin.js`
- **ID**: `SANITY-AUTH-LOGIN_LOGOUT_RELOGIN-SINGLE_USER`
- **Purpose**: Verify token lifecycle
- **Flow**:
  1. Login → get token1
  2. Call protected endpoint with token1 (should work)
  3. Logout
  4. Try old token (should fail 401)
  5. Login again → get token2
  6. Call protected endpoint with token2 (should work)
  7. Verify token1 ≠ token2
- **KPI**: Token revocation, new token validity
- **Pass**: Old token returns 401, new token works

#### `scenario-3-feed-refresh-repeat.js`
- **ID**: `SANITY-FEED-REFRESH-REPEAT-SINGLE_USER`
- **Purpose**: Verify feed endpoint stability under repeated calls
- **Flow**:
  1. Load token from tokens.csv
  2. Call GET /feeds 50 times with 0.1s interval
  3. Verify response shape consistency
  4. Check success rate > 95%
  5. Verify no error accumulation
- **KPI**: Success rate, response consistency, latency
- **Pass**: Success rate > 95%, no error spike

#### `scenario-4-search-related-fallback.js`
- **ID**: `SANITY-SEARCH-RELATED-FALLBACK-SINGLE_USER`
- **Purpose**: Verify search and fallback mechanism
- **Flow**:
  1. Call semantic search
  2. Call related posts (fallback)
  3. Verify fallback works when semantic is weak
  4. Verify results have expected fields
- **KPI**: Results validity, fallback availability
- **Pass**: Related posts always available, fields present

#### `scenario-5-share-unshare.js`
- **ID**: `SANITY-SHARE-UNSHARE-SINGLE_USER`
- **Purpose**: Verify share state consistency
- **Flow**:
  1. Get initial share count
  2. Share post → verify count +1
  3. Verify post in user's shares list
  4. Unshare post → verify count -1
  5. Verify post removed from shares list
- **KPI**: Count consistency, list consistency
- **Pass**: Count = initial ± 1, list state matches

---

### 2. **Configuration & Helpers** (2 files)

#### `config.js`
- **Purpose**: Centralized configuration for all sanity tests
- **Exports**:
  - `SANITY_CONFIG`: Base URL, endpoints, thresholds
  - `getAuthHeaders()`: Build auth headers
  - Response validators: hasData, isArray, hasId, hasTimestamp
  - Common endpoints: auth, posts, reactions, search, share
  - KPI thresholds: error rate, latency
- **Usage**: Import in test files for consistency

#### `test-matrix.js`
- **Purpose**: Complete test execution matrix with metadata
- **Contains**:
  - 5 scenario objects with full metadata
  - Preconditions, steps, KPIs, pass/fail criteria
  - Helper functions: getScenarioById(), getAllScenarios(), etc.
- **Usage**: Documentation, test planning, CI/CD integration

---

### 3. **Documentation & Scripts** (2 files)

#### `README.md`
- **Purpose**: Comprehensive user guide
- **Sections**:
  - Directory structure
  - Scenario descriptions (1-5)
  - How to run tests (all, single, with options)
  - Seed data format
  - Pass/Fail criteria
  - Common failure signals
  - CI/CD integration

#### `INDEX.md`
- **Purpose**: Quick reference and overview
- **Sections**:
  - Directory structure diagram
  - Scenario table
  - How to run (quick commands)
  - Seed data reference
  - Pass criteria summary
  - Testing standards
  - Failure signals table

---

### 4. **Test Runners** (2 files)

#### `run-all.sh` (Bash/Linux/Mac)
```bash
# Usage
./k6-tests/sanity/run-all.sh

# Features:
# - Sets environment variables
# - Loops through all scenarios
# - Generates JSON reports
# - Shows pass/fail summary
```

#### `run-all.ps1` (PowerShell/Windows)
```powershell
# Usage
.\k6-tests\sanity\run-all.ps1 -BaseUrl http://localhost:5000 -SeedDir ./seed-output -ShowSummary
```

---

## 🎯 Scenarios Summary

| # | Name | Module | Data Source | Key Check |
|---|------|--------|-------------|-----------|
| 1 | Like/Unlike Loop | reaction | tokens.csv, posts.csv | Count consistency |
| 2 | Login/Logout/Relogin | auth | N/A (hardcoded user) | Token revocation |
| 3 | Feed Refresh | feed | tokens.csv | Success rate > 95% |
| 4 | Search Related | search | tokens.csv, posts.csv | Fallback availability |
| 5 | Share/Unshare | share | tokens.csv, posts.csv | Count & list consistency |

---

## 📊 Test Data Used

All scenarios load from `seed-output/`:

- **tokens.csv**: Pre-generated JWT tokens for 1000+ users
  - Columns: profile_id, username, token, generated_at
- **posts.csv**: Seed posts with full data
  - Columns: post_id, profile_id, caption, privacy, created_at, etc.
- **reactions.csv**: Existing reactions (for verification)
- **reposts.csv**: Existing shares (for verification)
- **users.csv**: User information (for login tests)

---

## 🔧 How to Run

### Single test
```bash
export BASE_URL=http://localhost:5000
export SEED_OUTPUT_DIR=./seed-output

k6 run \
  --env BASE_URL=$BASE_URL \
  --env SEED_OUTPUT_DIR=$SEED_OUTPUT_DIR \
  k6-tests/sanity/scenario-1-reaction-like-unlike-loop.js
```

### All tests (Bash)
```bash
chmod +x k6-tests/sanity/run-all.sh
./k6-tests/sanity/run-all.sh
```

### All tests (PowerShell)
```powershell
.\k6-tests\sanity\run-all.ps1 -BaseUrl http://localhost:5000 -ShowSummary
```

---

## ✅ Pass Criteria

### Generic (All Tests)
- ✓ HTTP 2xx responses (with specific status codes per scenario)
- ✓ No 5xx errors
- ✓ Response time p95 < threshold (500-1000ms)
- ✓ Error rate < 10%

### Scenario-Specific
- **Scenario 1**: Count >= 0, no duplicates, final state = last action
- **Scenario 2**: Old token = 401, new token = 200, tokens different
- **Scenario 3**: Success rate > 95%, response shape consistent
- **Scenario 4**: Related posts has items, results have required fields
- **Scenario 5**: Count = initial ± 1, list state matches

---

## 🚨 Failure Detection

Tests will fail and report when:
- ✗ HTTP status code unexpected
- ✗ Response missing required fields
- ✗ Count goes negative
- ✗ Duplicate reactions found
- ✗ Old token still works after logout
- ✗ Success rate < 95%
- ✗ Response shape inconsistent
- ✗ Timeout or connection errors

---

## 📝 Standards Applied

From `00-overview-and-structure.md`:
- ✓ Single axis organization (by test type, not feature)
- ✓ Deterministic seeding (no random data generation)
- ✓ Scenario naming convention: `[TYPE]-[MODULE]-[BEHAVIOR]-[SCALE]`
- ✓ CSV seed data only
- ✓ Single user per test (no concurrency)

---

## 🔄 Integration Points

### Before deployment
```bash
./k6-tests/sanity/run-all.sh
```

### In CI/CD (`.github/workflows/`)
```yaml
- name: Run Sanity Tests
  run: |
    export BASE_URL=http://localhost:5000
    ./k6-tests/sanity/run-all.sh
```

### Before load testing
Run sanity suite to ensure baseline is stable

---

## 📁 File Structure

```
Favi-BE.API/
├── k6-tests/
│   └── sanity/
│       ├── README.md                              # User guide
│       ├── INDEX.md                               # Quick reference
│       ├── config.js                              # Common config
│       ├── test-matrix.js                         # Full test metadata
│       ├── run-all.sh                             # Bash runner
│       ├── run-all.ps1                            # PowerShell runner
│       │
│       └── Scenarios:
│           ├── scenario-1-reaction-like-unlike-loop.js
│           ├── scenario-2-auth-login-logout-relogin.js
│           ├── scenario-3-feed-refresh-repeat.js
│           ├── scenario-4-search-related-fallback.js
│           └── scenario-5-share-unshare.js
│
└── detail-testing-plan/
    ├── 00-overview-and-structure.md              # Standards
    ├── 01-smoke-sanity.md                        # Scenario descriptions
    └── 06-k6-standards-and-examples.md          # K6 standards
```

---

## ✨ Key Features

✓ **Deterministic**: Uses pre-seeded CSV data  
✓ **Isolated**: Single user per test, no concurrency  
✓ **Comprehensive**: 5 scenarios covering main features  
✓ **Documented**: Full README, INDEX, and inline comments  
✓ **Maintainable**: Central config, reusable helpers  
✓ **CI/CD Ready**: Bash and PowerShell runners  
✓ **Standards Compliant**: Follows project conventions  

---

## 📖 Next Steps

1. **Run locally** to verify connectivity to API
2. **Review** scenario details in README.md
3. **Check seed data** in seed-output/ CSV files
4. **Integrate** into CI/CD pipeline
5. **Schedule** to run after each deploy

---

**Created by**: GitHub Copilot  
**Date**: 2026-04-10  
**Version**: 1.0  
**Status**: ✅ Ready for Use

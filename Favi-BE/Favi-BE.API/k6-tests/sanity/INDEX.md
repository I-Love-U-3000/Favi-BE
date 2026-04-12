# K6 Tests - Sanity Test Suite

## 📁 Cấu trúc Thư mục

```
Favi-BE.API/
├── k6-tests/
│   └── sanity/
│       ├── README.md                                  # Hướng dẫn chi tiết
│       ├── config.js                                  # Configuration chung
│       ├── test-matrix.js                             # Test execution matrix
│       ├── run-all.sh                                 # Script chạy tất cả test
│       │
│       └── Scenarios:
│           ├── scenario-1-reaction-like-unlike-loop.js
│           ├── scenario-2-auth-login-logout-relogin.js
│           ├── scenario-3-feed-refresh-repeat.js
│           ├── scenario-4-search-related-fallback.js
│           └── scenario-5-share-unshare.js
│
├── seed-output/                                       # Test data (CSV)
│   ├── users.csv
│   ├── tokens.csv
│   ├── posts.csv
│   ├── reactions.csv
│   ├── reposts.csv
│   └── ... (other seed data)
│
└── detail-testing-plan/                              # Planning documents
    ├── 00-overview-and-structure.md                  # Overview
    ├── 01-smoke-sanity.md                            # Sanity scenarios (no code)
    └── 06-k6-standards-and-examples.md              # K6 standards
```

---

## 🎯 Sanity Test Scenarios

| # | Scenario | File | Module | Purpose |
|---|----------|------|--------|---------|
| 1 | **Like/Unlike Loop** | `scenario-1-reaction-like-unlike-loop.js` | `reaction` | Toggle like/unlike 10 times, verify count consistency |
| 2 | **Login/Logout/Relogin** | `scenario-2-auth-login-logout-relogin.js` | `auth` | Token lifecycle, old token revocation |
| 3 | **Feed Refresh** | `scenario-3-feed-refresh-repeat.js` | `feed` | Refresh feed 50 times, verify stability |
| 4 | **Search & Fallback** | `scenario-4-search-related-fallback.js` | `search` | Semantic search + related posts fallback |
| 5 | **Share/Unshare** | `scenario-5-share-unshare.js` | `share` | Share post, verify count & list consistency |

---

## 🔧 Cách chạy

### 1. Chạy tất cả scenario sanity
```bash
cd Favi-BE.API
export BASE_URL=http://localhost:5000
export SEED_OUTPUT_DIR=./seed-output

# Make script executable (Linux/Mac)
chmod +x k6-tests/sanity/run-all.sh

# Run
./k6-tests/sanity/run-all.sh
```

### 2. Chạy một scenario cụ thể
```bash
k6 run \
  --env BASE_URL=http://localhost:5000 \
  --env SEED_OUTPUT_DIR=./seed-output \
  k6-tests/sanity/scenario-1-reaction-like-unlike-loop.js
```

### 3. Chạy với kết quả chi tiết
```bash
k6 run \
  --env BASE_URL=http://localhost:5000 \
  --env SEED_OUTPUT_DIR=./seed-output \
  --out json=test-results/sanity-result.json \
  --summary-trend-stats="min,max,avg,p(95),p(99),p(99.9),count" \
  k6-tests/sanity/scenario-1-reaction-like-unlike-loop.js
```

---

## 📊 Dữ liệu Test

### Seed Data
Tất cả test dùng dữ liệu từ `seed-output/`:
- **tokens.csv**: Pre-generated JWT tokens cho 1000+ users
- **users.csv**: User info (profile_id, username, password)
- **posts.csv**: Seed posts với data đầy đủ
- **reactions.csv**: Existing reactions (để verify state)
- **reposts.csv**: Existing shares

### Format dữ liệu

**tokens.csv**:
```csv
profile_id,username,token,generated_at
000c735a-32e4-6fc8-0422-0bd3d4a8683a,user_02877,eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...,2026-04-10T11:31:33Z
```

**posts.csv**:
```csv
post_id,profile_id,caption,privacy,created_at,updated_at,is_archived,is_nsfw,location
e76e6cac-a3a9-41b7-b9f9-905c72182209,e1e3d83e-bfb7-6340-6f43-26c3737c2886,Seed post #1,Public,...
```

---

## ✅ Pass Criteria

Mỗi scenario có **Pass/Fail criteria** rõ ràng. Test PASS khi:

### Scenario 1 (Like/Unlike):
- ✓ Reaction count không âm
- ✓ Không duplicate reactions
- ✓ Final state = last action
- ✓ All requests return 2xx

### Scenario 2 (Auth):
- ✓ Login returns 200 with token
- ✓ Protected endpoint works with new token
- ✓ Old token returns 401 after logout
- ✓ Tokens are different

### Scenario 3 (Feed):
- ✓ Success rate > 95% (47/50)
- ✓ Response time p95 < 1000ms
- ✓ Response shape consistent
- ✓ No error spike

### Scenario 4 (Search):
- ✓ Search/related endpoints return 200
- ✓ Related posts provides fallback
- ✓ Results have post_id, caption, created_at

### Scenario 5 (Share):
- ✓ Share count +1 after share
- ✓ Post in user's shares list
- ✓ Share count restored after unshare
- ✓ Post removed from shares list

---

## 🚨 Common Failure Signals

| Issue | Cause | Check |
|-------|-------|-------|
| Count goes negative | DB bug or race condition | `count < 0` |
| Duplicate reactions | Missing unique constraint | Check reactions list |
| Old token still works | Token revocation not implemented | POST logout, then try old token |
| Count inconsistent | Query/update mismatch | GET before/after each action |
| Feed timeout | Query performance | p95 latency |
| Share count wrong | Calculation error | Seed data vs actual |

---

## 📝 Testing Standards (từ file 0 & 6)

### Quy chuẩn từ 00-overview-and-structure.md:
- **Single axis**: Tổ chức theo loại test (smoke, sanity, functional, load...)
- **Deterministic**: Dùng seeded data, không random
- **No media**: Tách biệt read/write test
- **Module independent**: Mọi module (auth, feed, post, reaction...) nằm trong cùng test type

### Quy chuẩn Scenario naming:
```
[TEST_TYPE]-[MODULE]-[BEHAVIOR]-[SCALE]
```

Ví dụ:
- `SANITY-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER`
- `SMOKE-AUTH-LOGIN-SINGLE_USER`
- `LOAD-FEED-REFRESH_HOTSPOT-MASS_USERS`

### Quy chuẩn Test Data:
- CSV format từ seed pipeline
- Deterministic seeding (Zipf distribution)
- Validator layer trước test
- Không tạo data random trong test

---

## 🔄 Workflow Integration

### Before committing:
```bash
./k6-tests/sanity/run-all.sh
```

### In CI/CD pipeline (`.github/workflows/test.yml`):
```yaml
- name: Run Sanity Tests
  run: |
    export BASE_URL=http://localhost:5000
    export SEED_OUTPUT_DIR=./seed-output
    ./k6-tests/sanity/run-all.sh
```

### Before performance testing:
Chạy sanity trước để ensure hệ thống ổn định base

---

## 📖 Tham khảo

- **Planning**: Xem `01-smoke-sanity.md` cho chi tiết scenario
- **Standards**: Xem `00-overview-and-structure.md` cho quy chuẩn
- **Seed Data**: Xem `seed-output/seed-manifest.json` cho manifest
- **Test Matrix**: Xem `test-matrix.js` cho KPI & criteria

---

## 🛠️ Development

Khi thêm scenario mới:

1. Thêm vào `detail-testing-plan/01-smoke-sanity.md` (description)
2. Tạo `k6-tests/sanity/scenario-X-name.js` (code)
3. Update `test-matrix.js` (metadata)
4. Update `run-all.sh` (add to array)
5. Update README này

---

**Tạo bởi**: Copilot  
**Ngày**: 2026-04-10  
**Version**: 1.0

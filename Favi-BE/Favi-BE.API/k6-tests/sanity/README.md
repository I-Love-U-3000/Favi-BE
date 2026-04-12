# Sanity Test Scenarios

## Cấu trúc thư mục

```
k6-tests/
├── sanity/
│   ├── config.js                                  # Configuration chung
│   ├── run-all.sh                                 # Script chạy tất cả test
│   ├── scenario-1-reaction-like-unlike-loop.js   # SANITY-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER
│   ├── scenario-2-auth-login-logout-relogin.js  # SANITY-AUTH-LOGIN_LOGOUT_RELOGIN-SINGLE_USER
│   ├── scenario-3-feed-refresh-repeat.js        # SANITY-FEED-REFRESH-REPEAT-SINGLE_USER
│   ├── scenario-4-search-related-fallback.js    # SANITY-SEARCH-RELATED-FALLBACK-SINGLE_USER
│   ├── scenario-5-share-unshare.js              # SANITY-SHARE-UNSHARE-SINGLE_USER
│   └── README.md                                 # This file
```

## Các Scenario Test

### 1. SANITY-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER
**Mục tiêu**: Đảm bảo toggle like/unlike ổn định

**Luồng**:
1. Like một post đã seed
2. Unlike post
3. Lặp lại 10 lần
4. Kiểm tra count không âm, không lệch
5. Verify không duplicate reaction
6. Verify trạng thái cuối phản ánh thao tác cuối

**File**: `scenario-1-reaction-like-unlike-loop.js`

**KPI**:
- Status 200/201 cho like request
- Status 200/204 cho unlike request
- Count không âm
- Count consistency

---

### 2. SANITY-AUTH-LOGIN_LOGOUT_RELOGIN-SINGLE_USER
**Mục tiêu**: Xác nhận token lifecycle ổn định

**Luồng**:
1. Login -> nhận token 1
2. Call protected endpoint với token 1 (should work)
3. Logout
4. Try use old token (should be revoked, 401)
5. Login lại -> nhận token 2
6. Call protected endpoint với token 2 (should work)

**File**: `scenario-2-auth-login-logout-relogin.js`

**KPI**:
- Login status 200
- Tokens khác nhau
- Old token returns 401 sau logout
- New token works sau relogin

---

### 3. SANITY-FEED-REFRESH-REPEAT-SINGLE_USER
**Mục tiêu**: Refresh liên tục không lỗi

**Luồng**:
1. Refresh feed 50 lần liên tiếp
2. Verify response shape consistent
3. Verify success rate > 95%
4. Verify không tăng lỗi dần

**File**: `scenario-3-feed-refresh-repeat.js`

**KPI**:
- Success rate > 95%
- Response time P95 < 1000ms
- Response shape consistency
- Error count <= 3

---

### 4. SANITY-SEARCH-RELATED-FALLBACK-SINGLE_USER
**Mục tiêu**: Verify search và fallback mechanism

**Luồng**:
1. Call semantic search
2. Call related posts (fallback)
3. Verify fallback works khi semantic yếu
4. Verify results có expected fields

**File**: `scenario-4-search-related-fallback.js`

**KPI**:
- Search endpoints respond 200
- Related posts always có items (fallback)
- Results have proper fields (post_id, caption, created_at)

---

### 5. SANITY-SHARE-UNSHARE-SINGLE_USER
**Mục tiêu**: Verify share state consistency

**Luồng**:
1. Get initial share count
2. Share post (repost)
3. Verify share count tăng 1
4. Verify post appears in user's shares
5. Unshare post
6. Verify share count restore
7. Verify post removed from user's shares

**File**: `scenario-5-share-unshare.js`

**KPI**:
- Share/unshare status 200/201/204
- Share count +1 sau share
- Share count -1 sau unshare
- Shares list consistency

---

## Cách chạy Test

### Chạy tất cả scenario
```bash
# Set environment variables
export BASE_URL=http://localhost:5000
export SEED_OUTPUT_DIR=./seed-output

# Make script executable
chmod +x k6-tests/sanity/run-all.sh

# Run all tests
./k6-tests/sanity/run-all.sh
```

### Chạy một scenario cụ thể
```bash
k6 run \
  --env BASE_URL=http://localhost:5000 \
  --env SEED_OUTPUT_DIR=./seed-output \
  k6-tests/sanity/scenario-1-reaction-like-unlike-loop.js
```

### Chạy với custom configuration
```bash
k6 run \
  --env BASE_URL=http://localhost:5000 \
  --env SEED_OUTPUT_DIR=./seed-output \
  --out json=results.json \
  --summary-trend-stats="min,max,avg,p(95),p(99),p(99.9),count" \
  k6-tests/sanity/scenario-1-reaction-like-unlike-loop.js
```

## Dữ liệu Test

### Seed Data Sources
- `users.csv`: Danh sách user (username, password, profile_id)
- `tokens.csv`: Pre-generated JWT tokens cho mỗi user
- `posts.csv`: Seed posts với post_id, caption
- `reactions.csv`: Existing reactions để verify state

### Format CSV

**tokens.csv**:
```
profile_id,username,token,generated_at
000c735a-32e4-6fc8-0422-0bd3d4a8683a,user_02877,eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**posts.csv**:
```
post_id,profile_id,caption,privacy,created_at,updated_at,is_archived,is_nsfw,location
e76e6cac-a3a9-41b7-b9f9-905c72182209,e1e3d83e-bfb7-6340-6f43-26c3737c2886,Seed post #1,Public,...
```

---

## Expected Results

### Pass Criteria
✓ Tất cả requests return appropriate status codes  
✓ Response data có expected fields  
✓ State transitions ổn định (like -> unlike -> like)  
✓ Counter integrity (count không âm, không lệch)  
✓ No race conditions ở single user level  
✓ Token lifecycle xử lý đúng  
✓ Fallback mechanisms hoạt động  

### Common Failure Signals
✗ 500 errors trong log  
✗ Count không match expected values  
✗ Response shape không consistent  
✗ Old token still accessible sau logout  
✗ Duplicate reactions  
✗ Share count không match  
✗ Feed response timeout  

---

## Notes

- Tất cả test dùng **single user** để tránh race condition
- Test dùng **pre-seeded data** từ seed-output CSV
- Không tạo random data, duy trì deterministic seeding
- Token từ tokens.csv, không login trong test
- Thường chạy trước mỗi deploy hoặc sau major changes

---

## Integration với CI/CD

Thêm vào `.github/workflows/test.yml`:
```yaml
- name: Run Sanity Tests
  run: |
    export BASE_URL=http://localhost:5000
    export SEED_OUTPUT_DIR=./seed-output
    ./k6-tests/sanity/run-all.sh
```

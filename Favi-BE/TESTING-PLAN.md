# Testing & Validation Plan — CQRS Refactoring (Auth · Notifications · Engagement)

**Mục tiêu:** Xác minh 3 modules đã chuyển sang CQRS + Outbox Pattern vẫn đảm bảo:
1. **Architecture Boundaries** — không vi phạm module isolation và CQRS segregation.
2. **Command/Query Result Parity** — logic nghiệp vụ không bị vỡ so với trước khi refactor.

---

## Tổng quan luồng thực thi

```
Phase 1: Architecture Tests (static)
  └── dotnet test Favi-BE.ArchitectureTests
        ├── BuildingBlocks layer rules (existing)
        ├── Auth module CQRS rules
        ├── Engagement module CQRS rules
        ├── Notifications module isolation rules
        └── API layer controller rules

Phase 2: K6 Parity Tests (dynamic — user runs)
  └── dotnet run (start API background)
        ├── smoke-auth-login.js          → Auth.LoginCQRS parity
        ├── smoke-comment-create.js      → Engagement.CreateComment parity
        ├── smoke-reaction-like.js       → Engagement.ToggleReaction parity
        └── scenario-6-notification-*   → Notification side-effect parity
```

---

## Giai đoạn 1 — Architecture Tests

### Prerequisite: ArchitectureTests.csproj

**Vấn đề ban đầu:** Test project chỉ tham chiếu `BuildingBlocks`. Để scan assembly của 3 module mới,
cần thêm 3 `<ProjectReference>` vào `.csproj`. Đây là bước bắt buộc; nếu bỏ qua,
NetArchTest sẽ scan sai assembly và pass hết mà không test gì cả.

**Đã thực hiện:** Thêm references tới `Favi-BE.Modules.Auth`, `Favi-BE.Modules.Notifications`,
`Favi-BE.Modules.Engagement`.

---

### Rule Set A — Engagement CQRS Segregation

| Rule | Mô tả | API NetArchTest |
|------|-------|-----------------|
| ENG-01 | CommandHandlers không được phụ thuộc vào Queries namespace | `Types.InAssembly(engagementAssembly).That().ResideInNamespace("...Commands").ShouldNot().HaveDependencyOn("...Queries")` |
| ENG-02 | QueryHandlers không được phụ thuộc vào Commands namespace | Đối xứng với ENG-01 |
| ENG-03 | QueryHandlers không được dùng WriteModels | `.ShouldNot().HaveDependencyOn("...WriteModels")` |

**Lưu ý quan trọng — Giới hạn của NetArchTest:**
`IEngagementCommandRepository` và `IEngagementQueryReader` nằm chung namespace
`Application.Contracts`. NetArchTest không phân biệt được hai interface này ở namespace level.
Do đó Rule "CommandHandler không inject QueryReader" không thể enforce trực tiếp bằng NetArchTest
mà cần Roslyn Analyzer hoặc code review. Thay vào đó, ENG-01 enforce việc không cross-import
giữa `Commands` ↔ `Queries` namespace — đây là boundary thực sự quan trọng hơn.

---

### Rule Set B — Auth CQRS Segregation

| Rule | Mô tả |
|------|-------|
| AUTH-01 | Types trong Auth `Commands` namespace không depend vào Auth `Queries` namespace |
| AUTH-02 | Types trong Auth `Queries` namespace không depend vào Auth `Commands` namespace |

---

### Rule Set C — Module Boundary Isolation

| Rule | Mô tả |
|------|-------|
| MOD-01 | Engagement assembly không depend vào `Favi_BE.Modules.Auth` hoặc `Favi_BE.Modules.Notifications` |
| MOD-02 | Auth assembly không depend vào `Favi_BE.Modules.Engagement` hoặc `Favi_BE.Modules.Notifications` |
| MOD-03 | Notifications assembly không depend vào `Favi_BE.Modules.Engagement.Application` hoặc `Favi_BE.Modules.Auth.Application` |

**Lưu ý:** Notifications được phép depend vào Events của chính nó (`Domain.Events`).
Cross-module communication chỉ qua integration events trong `BuildingBlocks`.

---

### Rule Set D — API Controller Isolation

| Rule | Mô tả |
|------|-------|
| API-01 | Controllers (`Favi_BE.Controllers`) không được phụ thuộc trực tiếp vào `Favi_BE.API.Data.Repositories` namespace |

**Lưu ý quan trọng — Tại sao không ban toàn bộ `Favi_BE.API`:**
Các Adapter classes (`EngagementCommandRepositoryAdapter`, `AuthWriteRepositoryAdapter`, v.v.) nằm trong
`Favi_BE.API.Application.*` và bắt buộc phải trực tiếp dùng `AppDbContext` + `IUnitOfWork` — đó là
lý do tồn tại của chúng. Nếu ban toàn bộ `Favi_BE.API` assembly, rule sẽ fail do Adapters,
không phải do Controllers vi phạm. Vì vậy rule chỉ scope vào `Favi_BE.Controllers` namespace.

---

## Giai đoạn 2 — K6 Parity Tests

### Cảnh báo: Threshold mặc định quá lỏng

Các script smoke hiện có `http_req_failed: ['rate<0.6']` — nghĩa là 60% request fail vẫn PASS.
Khi dùng cho parity validation cần override threshold khi chạy lệnh:

```bash
k6 run --env PARITY_MODE=1 \
    -e BASE_URL=http://localhost:5000 \
    smoke-auth-login.js
```

Hoặc thêm flag `--http-debug` để thấy toàn bộ request/response.

---

### 2.1 — Auth: Login Parity

**Script:** `k6-tests/smoke/smoke-auth-login.js`

**Assertions hiện có:**
- `login status is 200` ✅
- `login response has accessToken` ✅
- `protected feed status is 200` ✅

**Đủ để prove parity:** Có — Auth CQRS chỉ có Login/Register/RefreshToken.
Token format được verify qua `accessToken !== undefined`.

---

### 2.2 — Engagement: Comment & Reaction Parity

**Scripts:**
- `k6-tests/smoke/smoke-comment-create.js`
- `k6-tests/smoke/smoke-reaction-like.js`

**Assertions hiện có:**
- Create comment → status 200, `id` field present ✅
- Read-after-write → `created comment appears in list` (verify bằng comment ID) ✅
- Like post → status 200 ✅
- Verify reaction count → `reactions.total` present ✅

**Đủ để prove parity:** Có — smoke-comment-create thực sự verify read-after-write consistency,
không chỉ status code. Đây là test mạnh nhất trong bộ.

---

### 2.3 — Notifications: Outbox Side-Effect Parity

**Script:** `k6-tests/functional/scenario-6-notification-sideeffects-functional.js`

**Vấn đề với assertions hiện có:**
```js
check(notiRes, {
    'notifications items array': (r) => Array.isArray(r.json('items')),
});
```
Array rỗng cũng pass → Outbox có thể broken hoàn toàn mà test vẫn xanh.

**Giới hạn cố hữu của K6 với Outbox:**
Outbox processor chạy async (thường 1-5 giây sau khi action). K6 không có built-in retry
với backoff. Nếu check ngay sau action, notification có thể chưa được flush từ Outbox.

**Cách verify thay thế (không cần sửa script):**
Sau khi chạy K6 scenario-6, kiểm tra thủ công:
```bash
# Check unread count tăng
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/notifications/unread-count

# Check DB outbox table
SELECT status, message_type, created_at FROM outbox_messages
WHERE status = 'Processed' ORDER BY created_at DESC LIMIT 10;
```

---

## Verification Checklist

```
Phase 1 — Architecture Tests
[ ] Thêm 3 ProjectReference vào ArchitectureTests.csproj
[ ] Viết EngagementModuleArchitectureTests.cs (ENG-01, ENG-02, ENG-03)
[ ] Viết AuthModuleArchitectureTests.cs (AUTH-01, AUTH-02)
[ ] Viết NotificationsModuleArchitectureTests.cs (MOD-03)
[ ] Viết ApiLayerArchitectureTests.cs (API-01) + module boundary rules (MOD-01, MOD-02)
[ ] dotnet test Favi-BE.ArchitectureTests → All GREEN

Phase 2 — K6 Parity Tests (user runs)
[ ] dotnet run (start Favi-BE.API)
[ ] k6 run smoke/smoke-auth-login.js → All checks PASS
[ ] k6 run smoke/smoke-comment-create.js → All checks PASS (kể cả read-after-write)
[ ] k6 run smoke/smoke-reaction-like.js → All checks PASS
[ ] k6 run functional/scenario-6-notification-sideeffects-functional.js → endpoint 200
[ ] Verify Outbox table có records status=Processed (thủ công hoặc query)
```

---

## Lý do từng quyết định thiết kế

| Quyết định | Lý do |
|------------|-------|
| Rule API-01 scope chỉ vào `Favi_BE.Controllers` | Adapter classes trong `Favi_BE.API.Application` bắt buộc phải dùng DB trực tiếp — đó là nhiệm vụ của chúng |
| Không enforce "CommandHandler không inject QueryReader" bằng NetArchTest | Hai interface cùng namespace, NetArchTest không phân biệt được; cần Roslyn Analyzer |
| Giữ nguyên K6 scripts hiện có, không sửa | Scripts đã có assertions đúng; chỉ cần chạy với awareness về threshold lỏng |
| Notification parity verify qua DB thay vì K6 assertion | Outbox async nature làm K6 timing unreliable; DB query là source of truth |

# 02 — Tầm nhìn & Roadmap

---

## 1. Vấn đề ban đầu cần giải quyết

Favi-BE bắt đầu như một monolith truyền thống:
- **21 controllers → 25 services → 24 repositories** tất cả gọi thẳng lẫn nhau.
- Side effects (SignalR push, HTTP external, email) được nhúng thẳng vào write transaction.
- Không có transaction boundary nhất quán: nhiều `CompleteAsync()` trong cùng 1 use-case.
- Cross-module coupling: `ProfileService.FollowAsync` gọi `INotificationService` trực tiếp — biên giới module là vô hình.
- Không có read/write segregation — query-heavy reads và write commands đi qua cùng tracking DbContext.

Hậu quả: **Bất kỳ thay đổi nhỏ nào cũng có nguy cơ ripple effect qua toàn codebase.**

---

## 2. Mục tiêu ngắn hạn (Phase 2 — Q2 2026)

> Hoàn thành các domain slices còn lại theo thứ tự an toàn.

| Mục tiêu | Tiêu chí đo lường |
|----------|-------------------|
| Migrate Engagement commands (comment, reaction) sang CQRS handlers | Reaction/comment correctness parity |
| Migrate SocialGraph commands (follow, unfollow) | Follow graph + notification parity |
| Migrate ContentPublishing commands (post, collection, repost) | Content mutation parity, zero regression |
| Bảo đảm mọi cross-boundary interaction đi qua outbox | Không còn direct cross-module service call |
| Read/write segregation contract tại tầng handler | Command handlers không gọi query readers |

---

## 3. Mục tiêu trung hạn (Phase 3 — Q3 2026)

> Hoàn thiện Stories, Messaging, Moderation; hardening toàn hệ thống.

| Mục tiêu | Ghi chú |
|----------|---------|
| Stories lifecycle + background expiry CQRS | Expiry background process cần InternalCommands hoặc Hangfire gate |
| Messaging: conversation/message read-write split | Hot path dùng read contracts AsNoTracking → Dapper migration seam |
| Moderation & Backoffice CQRS | Audit trail immutable, admin workflow parity |
| Xóa legacy `IAuthService` path | Chỉ khi parity ổn định ít nhất 1 release cycle |
| Enforce module boundaries tự động qua CI | Architecture tests fail-fast khi vi phạm |

---

## 4. Mục tiêu dài hạn (Phase 4 — Q4 2026+)

> Chuẩn bị nền tảng cho khả năng tách microservice nếu cần.

```
Modular Monolith (current target)
       │
       ▼
Module boundaries enforced by code + CI
       │
       ▼ (nếu scale cần thiết)
Extract high-traffic modules as microservices
(e.g., Notifications, Messaging chạy process riêng)
```

**Lý do không tách microservice ngay:**
- Overhead vận hành (distributed tracing, service mesh, saga) lớn hơn lợi ích ở quy mô hiện tại.
- Modular monolith với outbox/inbox đã có seam tự nhiên để tách sau — không cần rewrite, chỉ cần extract + deploy riêng.

---

## 5. Strangler Fig — cách tiếp cận migration

```
                    ┌─────────────────────┐
  HTTP Request ────▶│  AuthController     │ ← giữ nguyên API contract
                    └────────┬────────────┘
                             │ IMediator.Send(LoginCommand)     [NEW]
                             │ ─────────────────────────────▶ Handler
                             │
                  Legacy:    │ IAuthService.LoginAsync()        [OLD — còn đăng ký làm fallback]
                             │
```

- **Không xóa legacy ngay**: legacy path giữ làm rollback gate.
- **Additive only**: thêm handler mới, giữ old service đăng ký.
- **Rollback**: `git revert` về slice commit — legacy path quay lại hoạt động ngay.
- **Remove legacy**: chỉ sau khi slice ổn định ít nhất 1 release cycle + parity report clean.

---

## 6. Roadmap timeline (dự kiến)

```
Q2/2026   R4: Engagement.Commands
          R5: ContentPublishing.Commands

Q3/2026   R6: Stories.CommandsAndExpiry
          R7: Messaging.CQRS

Q3/Q4     R8: Moderation.BackofficeCQRS
          R9: Hardening, legacy removal, final parity signoff

Q4/2026+  Read-model migration seam (EF AsNoTracking → Dapper projection)
          Module boundary enforcement + CI gate mature
          Evaluate microservice extraction candidates
```

---

## 7. Non-goals (cố tình không làm trong scope này)

| Không làm | Lý do |
|-----------|-------|
| Tách microservice trong phase này | Overhead lớn, seam chưa đủ ổn định |
| Rewrite toàn bộ từ đầu | Strangler Fig bảo toàn giá trị đang hoạt động |
| Thêm Kafka/RabbitMQ ngay | Outbox in-process đủ cho quy mô hiện tại, message broker là option sau |
| Event sourcing | Không phù hợp với domain hiện tại, phức tạp không tương xứng |

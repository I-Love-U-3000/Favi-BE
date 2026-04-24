# 01 — Thống kê tiến độ (cập nhật 2026-04-24)

---

## 1. Tổng quan release milestones

| Release | Nội dung | Trạng thái |
|---------|----------|------------|
| R0 | Discovery + tài liệu kiến trúc 16 docs | ✅ Hoàn thành |
| R1 | Foundation.CQRSOutbox — BuildingBlocks, MediatR, Outbox/Inbox | ✅ Hoàn thành |
| R2 | Auth.LoginCQRS — module Auth, controller strangler | ✅ Hoàn thành |
| R3 | Notification.EventDriven — tách side-effect khỏi write path | ✅ Hoàn thành |
| R4 | Engagement.Commands + SocialGraph.Commands | ⬜ Chưa bắt đầu |
| R5 | ContentPublishing.Commands | ⬜ Chưa bắt đầu |
| R6 | Stories.CommandsAndExpiry | ⬜ Chưa bắt đầu |
| R7 | Messaging.CQRS | ⬜ Chưa bắt đầu |
| R8 | Moderation.BackofficeCQRS | ⬜ Chưa bắt đầu |
| R9 | Hardening + cleanup + final parity signoff | ⬜ Chưa bắt đầu |

**Tổng R0–R9: 10 milestones. Đã hoàn thành: 4/10 (40%).**

---

## 2. Tiến độ theo slice chi tiết

### Slice 0 — Foundation.CQRSOutbox ✅ DONE

| Hạng mục | Hoàn thành |
|----------|------------|
| BuildingBlocks project khởi tạo + tích hợp solution | ✅ |
| MediatR + pipeline behaviors (Validation → Logging → Performance → Transaction) | ✅ |
| Outbox/Inbox schema (`OutboxMessages`, `InboxMessages`) | ✅ |
| Domain events accessor từ EF ChangeTracker | ✅ |
| OutboxProcessor hosted service (retry + poison capture) | ✅ |
| InboxProcessor idempotent (dedup theo `messageId + consumerName`) | ✅ |
| Architecture tests baseline | ✅ |
| API modularization (AuthenticationExtensions, InfrastructureExtensions, ApplicationExtensions, StartupTasksExtensions) | ✅ |

**Tỷ lệ slice 0: 8/8 (100%)**

---

### Slice 1 — Auth.LoginCQRS ✅ DONE

| Hạng mục | Hoàn thành |
|----------|------------|
| `Favi-BE.Modules.Auth` project khởi tạo | ✅ |
| LoginCommand + handler | ✅ |
| RefreshTokenCommand + handler | ✅ |
| LogoutCommand + handler | ✅ |
| RegisterCommand + handler | ✅ |
| ChangePasswordCommand + handler | ✅ |
| RequestPasswordResetCommand | ⏸ Deferred (SMTP chưa có) |
| ResetPasswordCommand | ⏸ Deferred (phụ thuộc password-reset token store) |
| AuthController strangler (gọi IMediator.Send thay orchestration) | ✅ |
| Port/adapter pattern (IAuthWriteRepository, IJwtTokenService, IAuthQueryReader) | ✅ |
| AddAuthModule() extension | ✅ |
| AuthSession lifecycle | ⏸ Deferred (additive migration chưa chạy) |

**Tỷ lệ slice 1: 9/12 (75%) — 3 items deferred có decision log**

---

### Slice 2 — Notification.EventDriven ✅ DONE

| Hạng mục | Hoàn thành |
|----------|------------|
| `Favi-BE.Modules.Notifications` project khởi tạo | ✅ |
| Gỡ SignalR push trực tiếp khỏi write path | ✅ |
| Integration events: CommentCreated, UserFollowed, PostReactionToggled, CommentReactionToggled | ✅ |
| OutboxNotificationService (chỉ ghi OutboxMessage, zero side-effect) | ✅ |
| OutboxProcessor cập nhật: dispatch theo IInboxConsumer.MessageType | ✅ |
| 4 notification consumers (idempotent + persist + SignalR push) | ✅ |
| INotificationRealtimeGateway adapter (giữ nguyên event names: ReceiveNotification, UnreadCountUpdated) | ✅ |
| Inbox dedup (messageId + consumerName) | ✅ |
| Automated integration test chứng minh không còn hub push trong write path | ⬜ Còn thiếu |

**Tỷ lệ slice 2: 8/9 (89%) — integration test còn là gap**

---

### Slice 3–8 — Chưa bắt đầu

| Slice | Scope | Status |
|-------|-------|--------|
| Engagement.Commands | CreateComment, ToggleReaction | ⬜ |
| SocialGraph.Commands | Follow, Unfollow, SocialLink | ⬜ |
| ContentPublishing.Commands | Post/Collection/Repost CRUD | ⬜ |
| Stories.CommandsAndExpiry | Story lifecycle + expiry background | ⬜ |
| Messaging.CQRS | Conversation/Message split, read-model | ⬜ |
| Moderation.BackofficeCQRS | Report, Moderation, Admin audit | ⬜ |

---

## 3. Tổng hợp tỷ lệ hoàn thành

```
Slices hoàn chỉnh:      3 / 9    (33%)
Release milestones:     4 / 10   (40%)
Tài liệu kiến trúc:   16 / 16   (100%)
Discovery & baseline:  100%
Foundation infra:      100%
Auth module:            75%  (3 items deferred có log)
Notifications module:   89%  (1 integration test gap)
```

**Phase 1 (Discovery + Foundation + Auth + Notifications) = COMPLETED.**
**Phase 2 (Domain slices R4–R8) = NEXT — chưa bắt đầu.**

---

## 4. Những gì đã sản xuất thực sự (artifacts)

| Loại | Số lượng |
|------|----------|
| Tài liệu kiến trúc (CQRS-Modernization/) | 16 |
| Projects mới thêm vào solution | 3 (BuildingBlocks, Modules.Auth, Modules.Notifications) |
| MediatR command handlers đã migrate | 5 (Login, Register, Refresh, Logout, ChangePassword) |
| Notification consumers (event-driven) | 4 |
| Pipeline behaviors | 4 (Validation, Logging, Performance, Transaction) |
| Integration events defined | 4 |
| API extension classes (modularized DI) | 4 |
| Architecture test rules | 2+ (dependency direction, read/write segregation) |

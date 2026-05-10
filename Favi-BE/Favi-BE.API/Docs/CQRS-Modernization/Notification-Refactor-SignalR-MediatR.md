# Notification Refactor: SignalR + MediatR

## 1. State progression

### 1.1 Original state (pre-Slice 2)
- Controllers gọi service commands (`CommentService`, `ProfileService`, `PostService`).
- Service commands persist domain change, sau đó gọi `INotificationService.Create*NotificationAsync(...)` trực tiếp trong cùng flow.
- `NotificationService` vừa persist `Notification` entity vừa push SignalR (`IHubContext<NotificationHub>`) trong cùng request — in-transaction hub push.
- `CommentService.ToggleReactionAsync` dùng `Task.Run(...)` để fire-and-forget notification — không có retry, không có idempotency.

### 1.2 Current state (Slice 2 ✅ Done)
- DI registration chuyển từ `NotificationService` sang `OutboxNotificationService`.
- `OutboxNotificationService.Create*NotificationAsync(...)`: resolve actor/recipient data, tạo integration event, ghi vào `OutboxMessages` table qua `IOutbox.AddAsync` + `SaveChangesAsync` — không push SignalR trực tiếp.
- `OutboxProcessor` (hosted service) poll `OutboxMessages` mỗi 5 giây, dispatch tới `IInboxConsumer` tương ứng theo `MessageType`.
- 4 consumers trong `Favi-BE.Modules.Notifications`: `UserFollowedNotificationConsumer`, `CommentCreatedNotificationConsumer`, `PostReactionToggledNotificationConsumer`, `CommentReactionToggledNotificationConsumer`.
- Mỗi consumer: idempotent dedup qua `IInbox.TryStartProcessingAsync(messageId, consumerName)`, persist `Notification` entity, push SignalR qua `INotificationRealtimeGateway` adapter.
- Cross-module glue hiện tại: `SocialGraph` handlers gọi `ISocialGraphNotificationService` → `SocialGraphNotificationServiceAdapter` → `OutboxNotificationService`. `Engagement` handlers gọi `IEngagementNotificationService` → `EngagementNotificationServiceAdapter` → `OutboxNotificationService`. Đây là intermediate step, không phải target cuối.

### 1.3 Target state (Slice 13 ⏳ Pending)
- `SocialGraph`/`Engagement` handlers không gọi notification service adapter.
- Handlers raise domain events trên aggregate (`UserFollowedDomainEvent`, `CommentCreatedDomainEvent`, `ReactionToggledDomainEvent`).
- `TransactionBehavior` dispatch domain events sau `SaveChangesAsync` thành công.
- `DomainEventsDispatcher` map domain event → integration event → outbox message trong cùng transaction.
- `ISocialGraphNotificationService` và `IEngagementNotificationService` bị xóa.

---

## 2. Contract requirements
- Giữ nguyên SignalR event names cho client: `ReceiveNotification`, `UnreadCountUpdated`.
- Không có in-transaction hub push (đã đạt được ở Slice 2, ngoại trừ `MarkAllAsReadAsync` — acceptable vì không thuộc write transaction).
- Dedup sends với `messageId + consumerName` key (đã implement qua `IInbox`).

---

## 3. Refactor steps — trạng thái hiện tại

| Step | Mô tả | Status |
|---|---|---|
| 1 | Add domain events: `UserFollowedDomainEvent`, `CommentCreatedDomainEvent`, `ReactionToggledDomainEvent` | ⏳ Pending — Slice 13 |
| 2 | Map domain events → integration events → outbox trong `DomainNotificationsMapper` per module | ⏳ Pending — Slice 13 |
| 3 | Persist outbox messages in transaction | ✅ Done — `OutboxNotificationService.EnqueueOutboxAsync` → `IOutbox.AddAsync` + `SaveChangesAsync` |
| 4 | Notification handlers (consumers) persist notification + push SignalR | ✅ Done — 4 `IInboxConsumer` implementations trong `Favi-BE.Modules.Notifications` |
| 5 | Use `INotificationRealtimeGateway` adapter for SignalR (không inject `IHubContext` trực tiếp vào consumers) | ✅ Done — `NotificationRealtimeGatewayAdapter` trong `Favi-BE.API/Application/Notifications` |
| 6 | Idempotency guard trong notifications module | ✅ Done — `IInbox.TryStartProcessingAsync(messageId, consumerName, ...)` mỗi consumer |

**Lưu ý về Steps 1–2:** Hiện tại notification events được tạo trong `OutboxNotificationService` (API layer) bằng cách resolve actor/recipient data từ `IUnitOfWork`. Target là các events này phải được raise từ domain aggregate trong module (không cần data resolution ở API layer), sau đó dispatcher map sang integration event và ghi outbox. Steps 1–2 chưa làm vì cần Slice 13.

---

## 4. Testing requirements
- Integration test: không có `IHubContext` call nào xảy ra trước transaction commit trong CREATE notification path. _(⏳ chưa có automated test — ghi nhận trong Execution-Checklist Section 5 Exit criteria)_
- Idempotency test: cùng event replay nhiều lần không tạo duplicate notification. _(✅ có inbox dedup — chưa có automated test cụ thể)_
- Parity test: unread count sau refactor bằng legacy path. _(✅ manual validated)_
- Domain event test (Slice 13): sau khi bỏ notification adapter, verify handler chỉ raise domain event, không inject `ISocialGraphNotificationService`/`IEngagementNotificationService`. _(⏳ pending — Slice 14)_

---

## 5. Rollback trigger
- Duplicate sends, unread mismatch, hoặc error rate spike → revert slice commit.
- Slice 2 rollback: swap DI registration từ `OutboxNotificationService` về `NotificationService` trong `ApplicationExtensions`.
- Slice 13 rollback: revert domain event wiring, restore notification service adapter injection trong handlers.

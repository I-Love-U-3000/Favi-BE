# Notification Refactor: SignalR + MediatR

## 1. Current vs target
### Current
- Services gọi `NotificationService` trực tiếp.
- `NotificationService` vừa persist vừa push SignalR.

### Target
- Command raises domain event.
- In-process handler: same-module state only.
- Cross-boundary event -> Outbox.
- Outbox processor -> MediatR notification/integration event handler.
- Notification handler persist notification + push SignalR.

## 2. Contract requirements
- Preserve client event names: `ReceiveNotification`, `UnreadCountUpdated`.
- No in-transaction hub push.
- Dedup sends with message id + recipient key.

## 3. Refactor steps
1. Add domain events: `UserFollowedDomainEvent`, `CommentCreatedDomainEvent`, `ReactionToggledDomainEvent`.
2. Map domain events to integration notifications.
3. Persist outbox messages in transaction.
4. Create notification application handlers:
   - `CreateNotificationFromFollowHandler`
   - `CreateNotificationFromCommentHandler`
   - `CreateNotificationFromReactionHandler`
5. Use `INotificationRealtimeGateway` adapter for SignalR.
6. Add idempotency guard in notifications module.

## 4. Testing requirements
- Integration test: no `IHubContext` call before transaction commit.
- Idempotency test: same event replay does not duplicate notification.
- Parity test: unread count equals legacy path.

## 5. Rollback trigger
- Duplicate sends, unread mismatch, or error rate spike -> revert slice commit.
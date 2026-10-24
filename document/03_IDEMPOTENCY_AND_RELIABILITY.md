# 03 — Idempotency Architecture & Data Reliability (Favi-BE)

## 1. Two-Layer Idempotency Overview

In a high-throughput social network, users frequently double-click action buttons, mobile clients retry dropped HTTP connections, and asynchronous queues redeliver messages under at-least-once delivery guarantees. Favi-BE protects system state and data integrity via a **Two-Layer Idempotency Architecture**:

1. **Synchronous Layer (HTTP API Gateway / Middleware Level)**: Guards all state-mutating endpoints against duplicate HTTP requests from clients.
2. **Asynchronous Layer (Messaging / Consumer Level)**: Guards background Outbox/Inbox processors against redundant processing of integration events.

```
[ Layer 1: HTTP Request ]
Client ──▶ Header: Idempotency-Key ──▶ IdempotencyMiddleware (Redis Lock / Cache)
                                               │
                                      (If fresh & valid)
                                               ▼
                                      Command Handler & SaveChanges
                                               │ (Unified DB Transaction)
                                      ┌────────┴────────┐
                                      ▼                 ▼
                                [ Data State ]   [ Outbox Event ]
                                                        │
[ Layer 2: Event Processing ]                            ▼
OutboxProcessor ──▶ Inbox Pattern ──▶ IInbox.TryStartProcessing(msgId, consumer)
                                               │
                                      (If unprocessed)
                                               ▼
                                  Notification / Side Effect
```

---

## 2. Layer 1 Deep Dive: HTTP API `IdempotencyMiddleware`

Implemented at `Favi-BE.API/Middleware/IdempotencyMiddleware.cs`.

### 2.1 Header Enforcement Rules
- **Scope**: All state-mutating HTTP methods: `POST`, `PUT`, `PATCH`, `DELETE` targeting routes with the `/api/` prefix.
- **Required Header**: `Idempotency-Key` (a client-generated unique string, e.g., UUIDv4, max 128 characters).
- **Excluded Prefixes (Bypass list)**:
  - Authentication endpoints: `/api/auth/login`, `/api/auth/register`, `/api/auth/refresh-token`, `/api/auth/forgot-password`, `/api/auth/reset-password`.
  - Health checks & API Docs: `/health`, `/scalar`, `/openapi`.
  - Realtime WebSocket Hubs: `/chatHub`, `/notificationHub`, `/callHub`.
  - Batch operations / search: `/api/search`, `/api/posts/batch`, `/api/seed`.
  - Multipart file uploads (`context.Request.HasFormContentType = true`).

### 2.2 Error Codes & Responses
- Missing `Idempotency-Key` header: Returns `400 Bad Request` with error code `MISSING_IDEMPOTENCY_KEY`.
- Key length exceeds 128 characters: Returns `400 Bad Request` with error code `INVALID_IDEMPOTENCY_KEY`.

### 2.3 Distributed Locking Mechanism (Redis)
1. Redis Cache Key format:
   ```
   favi:idempotency:{userId}:{idempotencyKey}
   ```
   *(For unauthenticated requests, `userId` defaults to `"anon"`)*.

2. **Concurrency Race Condition Prevention**:
   - Upon request arrival, the middleware executes:
     ```csharp
     var acquired = await db.StringSetAsync(cacheKey, "IN_FLIGHT", TimeSpan.FromSeconds(60), When.NotExists);
     ```
   - If `acquired == false` (indicating an identical request key is actively being processed by another thread):
     The system immediately returns **`409 Conflict`** with payload:
     ```json
     {
       "statusCode": 409,
       "code": "IDEMPOTENT_OPERATION_IN_PROGRESS",
       "message": "The previous operation is currently in progress. Please wait."
     }
     ```

3. **Response Caching (Replay)**:
   - When the handler executes successfully with a `2xx` HTTP status code:
     The middleware serializes `(StatusCode, ContentType, ResponseBody)` and caches it in Redis with **TTL = 5 minutes** (`TimeSpan.FromMinutes(5)`).
   - If the client retries with the same `Idempotency-Key` within 5 minutes:
     The middleware intercepts the request and replays the cached response **without re-executing the handler or inserting duplicate records into the database**.

4. **Automatic Lock Release on Errors (Rollback on Failure)**:
   - If the handler returns a non-2xx status code (e.g., `400`, `500`) or throws an unhandled exception:
     The middleware immediately deletes the Redis key (`await db.KeyDeleteAsync(cacheKey)`).
   - **Rationale**: Allows the client to correct invalid inputs and retry immediately without being blocked by false-positive 409 conflicts.

---

## 3. Layer 2 Deep Dive: Messaging & Consumer Idempotency (Outbox + Inbox)

### 3.1 The Core Problem
Previously, operations like `PostService.ToggleReactionAsync` invoked `INotificationService` directly inside the database transaction. If the notification service timed out, the transaction failed; conversely, if SignalR emitted the push before the DB commit succeeded, ghost notifications were created.

### 3.2 Atomic Outbox Pattern
Every integration event is saved to the `OutboxMessages` table **inside the exact same DbContext SaveChanges / Database Transaction** as the aggregate root state change:

```csharp
// Inside Command Handler
_postRepository.Add(reaction);
await _outbox.AddAsync(new PostReactionToggledIntegrationEvent(postId, userId), cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken); // ATOMIC COMMIT
```

### 3.3 Consumer Idempotency via the Inbox Table
The background `OutboxProcessor` polls unhandled messages and publishes them to consumers. Because network delivery can produce duplicates, every consumer must guard execution with `IInbox`:

```csharp
public async Task Handle(PostReactionToggledIntegrationEvent notification, CancellationToken ct)
{
    var consumerName = nameof(PostReactionToggledNotificationConsumer);
    
    // 1. Idempotency Gate: Check composite lock (MessageId, ConsumerName)
    var started = await _inbox.TryStartProcessingAsync(notification.Id, consumerName, ct);
    if (!started)
    {
        // Already processed or actively in progress by this consumer -> safely exit
        return;
    }

    try
    {
        // 2. Execute side effect (Create notification, push SignalR)
        await _notificationRepository.AddAsync(...);
        await _realtimeGateway.PushNotificationAsync(...);

        // 3. Mark processed
        await _inbox.MarkProcessedAsync(notification.Id, consumerName, ct);
    }
    catch (Exception ex)
    {
        // Mark failed to allow retry on next poll cycle
        await _inbox.MarkFailedAsync(notification.Id, consumerName, ex.Message, ct);
        throw;
    }
}
```

**Architectural Invariant**: The composite key `(messageId, consumerName)` constitutes the unique idempotency boundary. A single event can be handled by multiple independent consumers without cross-consumer interference.

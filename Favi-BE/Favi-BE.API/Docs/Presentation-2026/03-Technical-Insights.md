# 03 — Technical Insights

---

## 1. Kiến trúc tổng thể

```
┌────────────────────────────────────────────────────────────────┐
│                        Favi-BE.API                             │
│   Controllers  ─▶  IMediator.Send()  ─▶  Pipeline Behaviors   │
│                                             │                  │
│   Legacy services (additive — rollback gate)│                  │
└────────────────────────────────────────────┼───────────────────┘
                                             │
        ┌────────────────┬──────────────────┬┴──────────────────┐
        │                │                  │                    │
   Favi-BE.       Favi-BE.          Favi-BE.           Favi-BE.
   Modules.       Modules.          Modules.           Modules.
   Auth           Notifications     [Engagement]       [...]
   (✅ done)      (✅ done)         (⬜ next)
        │                │
        └────────────────┴─────────────────────────────────────┐
                                                               │
                         Favi-BE.BuildingBlocks                │
                  Domain / Application / Infrastructure         │
                         (shared kernel)                       │
                                                               │
                    ┌──────────────────────────────────────────┘
                    │
             ┌──────┴──────┐
        OutboxProcessor  InboxProcessor
        (IHostedService) (IHostedService)
             │                │
             ▼                ▼
        OutboxMessages   InboxMessages
             (PostgreSQL / EF Core)
```

---

## 2. Pattern stack — lý do chọn từng pattern

### 2.1 CQRS (Command Query Responsibility Segregation)

**Vấn đề cũ:** Service methods vừa đọc dữ liệu phức tạp vừa mutate — không tách được concern, không scale được độc lập.

**Giải pháp:**
```
Command path:  HTTP → IMediator.Send(Command) → CommandHandler → Write DbContext (tracking)
                                                               → domain events → outbox

Query path:    HTTP → IMediator.Send(Query) → QueryHandler → Read contracts (AsNoTracking)
                                                           → (future: Dapper projections)
```

**Lợi ích thực tế:**
- Command handler có transaction boundary rõ ràng (1 aggregate = 1 transaction).
- Query handler không bao giờ mutate, dễ cache/scale read side.
- Dễ test từng handler độc lập.

---

### 2.2 Outbox Pattern

**Vấn đề cũ:** `PostService.ToggleReactionAsync` gọi `INotificationService` trực tiếp trong cùng transaction. Nếu notification fail, không có retry. Nếu transaction rollback sau khi notification gửi → duplicate hoặc inconsistency.

**Giải pháp:**
```
Write transaction:
  ┌──────────────────────────────────────────────────────┐
  │  1. Persist aggregate state (EF SaveChanges)         │
  │  2. Append OutboxMessage (cùng DbContext/transaction) │
  │  COMMIT                                              │
  └──────────────────────────────────────────────────────┘
  
  OutboxProcessor (background):
  ┌──────────────────────────────────┐
  │  Poll unprocessed OutboxMessages │
  │  Dispatch to IInboxConsumer      │
  │  Mark processed / failed         │
  │  Retry failed with backoff       │
  └──────────────────────────────────┘
```

**Invariant quan trọng:** Aggregate state và outbox message luôn atomically nhất quán — vì cùng 1 `SaveChanges()`.

---

### 2.3 Inbox Pattern (Idempotency guard)

**Vấn đề:** Outbox processor có thể retry → consumer bị gọi nhiều lần cho cùng 1 event.

**Giải pháp:**
```csharp
// Mỗi consumer check trước khi xử lý:
var started = await _inbox.TryStartProcessingAsync(messageId, consumerName, ct);
if (!started) return; // đã xử lý rồi, bỏ qua

// ... xử lý ...

await _inbox.MarkProcessedAsync(messageId, consumerName, ct);
```

Key: `(messageId, consumerName)` — một message có thể có nhiều consumer, mỗi cặp là idempotency unit riêng.

---

### 2.4 MediatR Pipeline Behaviors

```
Request ─▶ [ValidationBehavior] ─▶ [LoggingBehavior] ─▶ [PerformanceBehavior] ─▶ [TransactionBehavior*] ─▶ Handler
                                                                                         │
                                                                          * Chỉ cho commands (ICommand marker)
```

- **ValidationBehavior**: FluentValidation chạy trước handler, fail-fast với structured error.
- **LoggingBehavior**: Ghi request/response + correlation id, không làm ô nhiễm handler code.
- **PerformanceBehavior**: Cảnh báo khi handler chậm hơn ngưỡng.
- **TransactionBehavior**: Wrap command handler trong EF transaction, commit sau khi handler thành công, rollback nếu exception.

---

### 2.5 Strangler Fig Pattern — cơ chế migrate an toàn

**Nguyên tắc:** Không viết lại từ đầu. Xây handler mới song song, chuyển traffic từng phần, xóa legacy chỉ sau khi đủ tự tin.

```
Giai đoạn 1 (hiện tại):
  Controller → IMediator.Send(Command) → NEW handler
  Legacy IAuthService → vẫn đăng ký DI (rollback gate)

Giai đoạn 2 (sau parity signoff):
  Xóa DI registration cho legacy service
  Xóa legacy service class

Giai đoạn 3 (cleanup):
  Xóa legacy code, schema columns không còn dùng
```

**Rollback contract:** Mỗi slice là 1 git commit. `git revert <slice-commit-hash>` → legacy path quay lại hoạt động.

---

## 3. BuildingBlocks — Shared Kernel

Không phải "utilities" — là **domain contracts được chia sẻ**:

```
BuildingBlocks.Domain
├── Entity.cs              ← Base cho aggregate roots (holds domain events)
├── ValueObject.cs         ← Equality by value, immutable
├── IBusinessRule.cs       ← Domain invariant interface
├── IDomainEvent.cs        ← Marker cho domain events
├── TypedIdValueBase.cs    ← Strongly-typed IDs (chống nhầm UserId vs PostId)
└── BusinessRuleValidationException.cs

BuildingBlocks.Application
├── IOutbox.cs             ← Contract ghi outbox messages
├── IInbox.cs              ← Contract idempotency guard
├── IDomainEventNotification<T>.cs  ← Adapter domain event → MediatR notification
└── IExecutionContextAccessor.cs    ← User/correlation context

BuildingBlocks.Infrastructure
├── OutboxProcessor        ← IHostedService poll + dispatch
├── InboxProcessor         ← Consumer pipeline
├── DomainEventsAccessor   ← EF ChangeTracker scan for domain events
└── DomainEventsDispatcher ← In-process MediatR publish
```

**Rule cứng:** Domain không depend vào Application/Infrastructure. Application depend chỉ abstractions. Infrastructure implement abstractions.

---

## 4. Module Port/Adapter Pattern (Hexagonal tại module level)

Auth module không depend vào `AppDbContext` hay `JwtService` concrete — nó khai báo ports:

```
Favi-BE.Modules.Auth khai báo:
  IAuthWriteRepository   (port)
  IJwtTokenService       (port)
  IAuthQueryReader       (port)

Favi-BE.API implement:
  AuthWriteRepositoryAdapter   → AppDbContext/UnitOfWork
  JwtTokenServiceAdapter       → JwtService cũ
  AuthQueryReaderAdapter       → DbContext AsNoTracking
```

**Ý nghĩa:** Module Auth test được độc lập với fake ports. Khi đổi DB hay JWT library, chỉ đổi adapter — handler không động đến.

---

## 5. Notification flow — before vs after

### Before (monolith — tight coupling)
```
HTTP → PostService.ToggleReactionAsync()
         ├─ persist Reaction (CompleteAsync)
         └─ INotificationService.CreatePostReactionNotificationAsync()   ← IN TRANSACTION
                ├─ persist Notification (CompleteAsync)
                └─ IHubContext.Clients[userId].SendAsync("ReceiveNotification")  ← SIDE EFFECT IN TX
```

**Rủi ro:** Network call tới SignalR server trong transaction. Nếu commit fail sau push → ghost notification. Không có retry.

### After (event-driven — decoupled)
```
HTTP → TogglePostReactionCommand → Handler
         ├─ persist Reaction
         └─ IOutbox.AddAsync(PostReactionToggledIntegrationEvent)  ← atomic với Reaction persist
         COMMIT

OutboxProcessor (background):
  └─ PostReactionToggledNotificationConsumer
       ├─ IInbox.TryStartProcessing(messageId, consumerName)  ← idempotency gate
       ├─ persist Notification
       └─ INotificationRealtimeGateway.PushAsync("ReceiveNotification")  ← ngoài transaction
```

**Lợi ích:**
- Transaction boundary sạch — không có network I/O trong commit scope.
- Retry tự động nếu SignalR fail — outbox message chưa processed sẽ được poll lại.
- Không duplicate — inbox dedup theo `(messageId, consumerName)`.

---

## 6. Read/Write Segregation roadmap

```
Hiện tại (EF AsNoTracking cho read):
  QueryHandler → DbContext.Set<Post>().AsNoTracking().Where(...).Select(dto)

Tương lai (Dapper projection cho hot paths):
  QueryHandler → IDapperQueryReader.QueryAsync<PostSummaryDto>(sql, params)
```

Migration seam đã được thiết kế: mỗi query có interface `I<Module>QueryReader` — implementation có thể swap từ EF sang Dapper mà không đổi handler.

---

## 7. API modularization — DI organization

Program.cs từ "god class" → composition root gọn:

```csharp
// Program.cs (sau modularization)
builder.Services
    .AddAuthentication(builder.Configuration)   // JWT, policies
    .AddInfrastructure(builder.Configuration)   // EF, HealthChecks, CORS
    .AddApplication(builder.Configuration)      // MediatR, Repositories, Services
    .AddAuthModule()                            // Modules.Auth ports/adapters
    .AddNotificationsModule();                  // Modules.Notifications consumers

app.UseStartupTasks();                          // Migration, Seed — separate concern
```

**Mục tiêu:** Mỗi `Add*()` là 1 deployment unit nhỏ có thể review độc lập.

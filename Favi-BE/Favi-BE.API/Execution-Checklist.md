# Execution Checklist — CQRS + Outbox + MediatR + Strangler (Favi-BE)

Mục tiêu tài liệu này là checklist thực thi chi tiết theo từng bước, theo đúng thứ tự slice đã thống nhất. Mỗi bước đều có tiêu chí hoàn thành và cổng kiểm soát rollback.

---

# ABSOLUTE: Quy tắc vận hành chung (áp dụng cho mọi slice)

- Không ghi đè thay đổi chưa commit không liên quan.
- Chỉ làm thay đổi nhỏ, có thể đảo ngược.
- Ưu tiên thay đổi additive trước (không xóa legacy path sớm).
- Mỗi slice phải theo nhịp: `implement -> validate runnable state -> merge`.
- Rollback chuẩn: `git revert` về commit ổn định gần nhất.
- Trigger rollback bắt buộc: error rate tăng vượt ngưỡng, duplicate side effects, mismatch dữ liệu.
- Luôn so sánh parity old/new cho command result và chỉ số read-model chính.

---

## 1) Discovery & Baseline (bắt buộc trước khi code)

### 1.1 Khảo sát cấu trúc hiện trạng
- [x] Liệt kê controller/service/repository/entity/hub hiện có.
- [x] Vẽ dependency map: API -> Service -> Repository -> DbContext.
- [x] Ghi rõ các gọi chéo module trực tiếp.

#### Discovery snapshot — Current state inventory

- Controllers (`Favi-BE.API/Controllers`) — 21: `AdminAnalyticsController`, `AdminAuditController`, `AdminBulkController`, `AdminCommentsController`, `AdminContentController`, `AdminExportController`, `AdminHealthController`, `AdminReportsController`, `AdminUsersController`, `AuthController`, `ChatController`, `CollectionsController`, `CommentsController`, `NotificationsController`, `PostsController`, `ProfilesController`, `ProfileSyncController`, `ReportsController`, `SearchController`, `StoriesController`, `TagsController`.
- Services (`Favi-BE.API/Services`) — 25: `AnalyticsService`, `AuditService`, `AuthService`, `BulkActionService`, `ChatRealtimeService`, `ChatService`, `CloudinaryService`, `CollectionService`, `CommentService`, `ExportService`, `JwtService`, `NotificationService`, `NSFWService`, `PostCleanupService`, `PostService`, `PrivacyGuard`, `ProfileService`, `ReportService`, `SearchService`, `StoryExpirationService`, `StoryService`, `SystemMetricsService`, `TagService`, `UserModerationService`, `VectorIndexService`.
- Repositories (`Favi-BE.API/Data/Repositories`) — 24 + `GenericRepository<T>`: `AdminActionRepository`, `CollectionRepository`, `CommentRepository`, `ConversationRepository`, `EmailAccountRepository`, `FollowRepository`, `MessageReadRepository`, `MessageRepository`, `NotificationRepository`, `PostCollectionRepository`, `PostMediaRepository`, `PostRepository`, `PostTagRepository`, `ProfileRepository`, `ReactionRepository`, `ReportRepository`, `RepostRepository`, `SocialLinkRepository`, `StoryRepository`, `StoryViewRepository`, `TagRepository`, `UserConversationRepository`, `UserModerationRepository`.
- Entities (`Favi-BE.API/Models/Entities` + `JoinTables`) — 23: `AdminAction`, `Collection`, `Comment`, `Conversation`, `EmailAccount`, `Follow`, `Message`, `MessageRead`, `Notification`, `Post`, `PostCollection`, `PostMedia`, `PostTag`, `Profile`, `Reaction`, `Report`, `Repost`, `SocialLink`, `Story`, `StoryView`, `Tag`, `UserConversation`, `UserModeration`.
- Hubs (`Favi-BE.API/Hubs`): `NotificationHub`, `ChatHub`, `CallHub`.

#### Dependency map (as-is)

- `Controller` -> `I*Service` (DI trong `Program.cs`).
- `Service` -> `IUnitOfWork` + một số service chéo (`INotificationService`, `IPrivacyGuard`, `IVectorIndexService`, `INSFWService`, `ICloudinaryService`).
- `UnitOfWork` -> concrete repositories.
- Repositories -> `AppDbContext`.
- Commit tập trung qua `IUnitOfWork.Complete()/CompleteAsync()` -> `AppDbContext.SaveChanges()/SaveChangesAsync()`.

#### Direct cross-module calls (as-is)

- `ProfileService.FollowAsync` -> `INotificationService.CreateFollowNotificationAsync`.
- `CommentService.CreateAsync` -> `INotificationService.CreateCommentNotificationAsync`.
- `PostService.ToggleReactionAsync` -> `INotificationService.CreatePostReactionNotificationAsync`.
- `CommentService.ToggleReactionAsync` dùng `Task.Run(...)` để gọi notification bất đồng bộ ngoài flow chính.
- `SearchService` + `PostService` gọi `IVectorIndexService` (HTTP ngoài).
- `PostService` + `StoryService` gọi `INSFWService` (HTTP ngoài).

### 1.2 Transaction boundary & side effects
- [x] Xác định tất cả điểm commit (`UnitOfWork`, `SaveChanges`).
- [x] Xác định side effects trong transaction (SignalR, HTTP ngoài, email...).
- [x] Chụp luồng Notification hiện tại từ HTTP -> persistence -> hub push.

#### Transaction boundaries (as-is)

- Điểm commit chuẩn: `IUnitOfWork.Complete()/CompleteAsync()` (delegates to EF `SaveChanges`).
- `BeginTransactionAsync/CommitTransactionAsync/RollbackTransactionAsync` có trong `UnitOfWork` nhưng chưa được dùng rộng trong service commands.
- Nhiều command methods gọi nhiều lần `CompleteAsync()` trong cùng request (ví dụ media/NSFW/update flows), chưa có transaction boundary theo aggregate rõ ràng.

#### Side effects in/around write path

- SignalR push trực tiếp trong `NotificationService` (`ReceiveNotification`, `UnreadCountUpdated`) ngay sau persistence.
- SignalR push chat qua `ChatRealtimeService` (group events).
- HTTP ngoài qua `VectorIndexService` và `NSFWService`.
- Cloudinary I/O trong `PostService`/`StoryService` trước hoặc sau commit tùy luồng.
- Chưa thấy outbox/inbox cho cross-boundary side effects.

#### Notification flow snapshot (as-is)

- `HTTP` (`CommentsController`/`ProfilesController`/`PostsController`) -> service command (`CommentService`/`ProfileService`/`PostService`).
- Service command persist domain change bằng `_uow.CompleteAsync()`.
- Service command gọi `INotificationService.Create*NotificationAsync(...)`.
- `NotificationService` persist `Notification` + `_uow.CompleteAsync()`.
- `NotificationService` push SignalR trực tiếp qua `NotificationHub` event name `ReceiveNotification` + `UnreadCountUpdated`.

### 1.3 Baseline chất lượng
- [ ] Build baseline xanh.
- [ ] Snapshot test baseline (unit/integration nếu có).
- [x] Chốt anti-pattern list hiện trạng.

#### Anti-pattern list (verified by code)

- Service orchestration + business logic dày trong service layer (chưa có command/query handlers).
- Cross-module service calls trực tiếp (không qua integration events/outbox).
- SignalR side-effect gắn trực tiếp write flow (chưa event-driven).
- Thiếu transaction boundary nhất quán cho multi-step writes (nhiều lần `CompleteAsync` trong 1 use-case).
- Read/write chưa tách: command paths vẫn đọc dữ liệu query-heavy từ cùng `UnitOfWork`.
- `Task.Run(...)` trong `CommentService.ToggleReactionAsync` để đẩy notification có nguy cơ mất observability/retry/idempotency.

### Exit criteria Discovery
- [x] Có current-state architecture map.
- [x] Có anti-pattern list xác minh được bằng code.
- [x] Đã xác nhận/cập nhật module + aggregate baseline thực tế.

---

## 2) Tài liệu kiến trúc nền (trước khi triển khai slice)

- [x] `Docs/CQRS-Modernization/Architecture-BoundedContexts.md`
- [x] `Docs/CQRS-Modernization/Aggregate-Inventory.md`
- [x] `Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md`
- [x] `Docs/CQRS-Modernization/CQRS-CommandQuery-Catalog.md`
- [x] `Docs/CQRS-Modernization/Auth-CQRS-Catalog.md`
- [x] `Docs/CQRS-Modernization/BuildingBlocks-Design.md`
- [x] `Docs/CQRS-Modernization/Schema-Transition-Plan.md`
- [x] `Docs/CQRS-Modernization/Folder-Restructure-Mapping.md`
- [x] `Docs/CQRS-Modernization/ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`
- [x] `Docs/CQRS-Modernization/Outbox-Implementation-Guide.md`
- [x] `Docs/CQRS-Modernization/Inbox-Implementation-Guide.md`
- [x] `Docs/CQRS-Modernization/Notification-Refactor-SignalR-MediatR.md`
- [x] `Docs/CQRS-Modernization/Module-Boundary-Enforcement.md`
- [x] `Docs/CQRS-Modernization/Strangler-Rollout-Plan.md`
- [x] `Docs/CQRS-Modernization/InternalCommands-Decision-Log.md`
- [x] `Execution-Checklist.md`

### Exit criteria
- [x] Tài liệu đầy đủ end-to-end, không trùng số bước.
- [x] Có mapping cụ thể entity/service/controller hiện tại -> module/aggregate mục tiêu.

### 2.x Decision note (boundary ownership)
- [x] Target ownership decision đã chốt: `GetFollowersQuery` và `GetFollowingsQuery` thuộc `Social Graph` (`FollowRelationship` aggregate), không thuộc `Identity & Access`.
- [ ] Khi vào phase refactor code, dời hẳn handler/reader ownership của 2 query này sang `Social Graph`, vẫn giữ backward-compatible API contract trong giai đoạn strangler transition.

### 2.y Mandatory reference checklist (phải đọc trước khi làm từng mục)

#### 2.y.1 Theo phase/section
- [ ] Discovery (Section 1): đã đọc `Execution-Checklist.md` + `Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md` + `Docs/CQRS-Modernization/Architecture-BoundedContexts.md`.
- [ ] Domain decomposition: đã đọc `Docs/CQRS-Modernization/Aggregate-Inventory.md` + `Docs/CQRS-Modernization/Architecture-BoundedContexts.md`.
- [ ] CQRS catalog: đã đọc `Docs/CQRS-Modernization/CQRS-CommandQuery-Catalog.md` + `Docs/CQRS-Modernization/Auth-CQRS-Catalog.md`.
- [x] Schema transition: đã đọc `Docs/CQRS-Modernization/Schema-Transition-Plan.md` + `Docs/CQRS-Modernization/Outbox-Implementation-Guide.md` + `Docs/CQRS-Modernization/Inbox-Implementation-Guide.md`.
- [ ] Boundary enforcement: đã đọc `Docs/CQRS-Modernization/Module-Boundary-Enforcement.md` + `Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md`.

#### 2.y.2 Theo slice triển khai
- [x] `Foundation.CQRSOutbox`: đã đọc `BuildingBlocks-Design.md` + `Outbox-Implementation-Guide.md` + `Inbox-Implementation-Guide.md` + `Schema-Transition-Plan.md`.
- [x] `Auth.LoginCQRS`: đã đọc `Auth-CQRS-Catalog.md` + `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md`.
- [x] `Notification.EventDriven`: đã đọc `Notification-Refactor-SignalR-MediatR.md` + `Outbox-Implementation-Guide.md` + `Inbox-Implementation-Guide.md`.
- [x] `Engagement.Commands`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Favi-Concrete-Module-Aggregate-Matrix.md`.
- [ ] `SocialGraph.Commands`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Architecture-BoundedContexts.md` + `Favi-Concrete-Module-Aggregate-Matrix.md`.
- [ ] `ContentPublishing.Commands`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Schema-Transition-Plan.md`.
- [ ] `Stories.CommandsAndExpiry`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Schema-Transition-Plan.md`.
- [ ] `Messaging.CQRS`: đã đọc `CQRS-CommandQuery-Catalog.md` + `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` + `Module-Boundary-Enforcement.md`.
- [ ] `Moderation.BackofficeCQRS`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Module-Boundary-Enforcement.md`.

---

## 3) Slice 0 — `Foundation.CQRSOutbox`

### 3.1 BuildingBlocks Project
- [x] Khởi tạo project `Favi-BE.BuildingBlocks` (.csproj) tại thư mục gốc.
- [x] Thêm project vào Solution và tham chiếu từ `Favi-BE.API`.
- [x] Di chuyển code từ `Favi-BE.API/BuildingBlocks` sang project mới.
- [x] Cấu hình `BuildingBlocks.Domain` (Entity, ValueObject, IBusinessRule, IDomainEvent, TypedIdValueBase).
- [x] Cấu hình `BuildingBlocks.Application` (IExecutionContextAccessor, IDomainEventNotification, outbox/inbox contracts).
- [x] Cấu hình `BuildingBlocks.Infrastructure` (domain events dispatching, outbox/inbox processors).

### 3.2 MediatR + pipeline
- [x] Đăng ký MediatR scan assemblies.
- [x] Thêm behaviors theo thứ tự: Validation -> Logging -> Performance -> Transaction(write).
- [x] Chuẩn hóa naming convention Command/Query/Handler.

### 3.3 Outbox/Inbox schema + infra
- [x] Thêm bảng `OutboxMessages`.
- [x] Thêm bảng `InboxMessages`.
- [x] (Optional) Quyết định `InternalCommands` (adopt/defer + rationale).
- [x] Domain events accessor từ EF ChangeTracker.
- [x] Implement `IDomainNotificationsMapper` (domain event -> application notification).
- [x] Mapping domain event -> domain notification -> outbox payload.
- [x] `IHostedService` xử lý outbox với retry + poison/error capture.
- [x] Pipeline inbox idempotent với dedup theo `MessageId + Consumer`.
- [x] `InboxProcessor`/consumer pipeline (receive -> dedup -> handler -> mark processed/failed).

### 3.4 Architecture tests baseline
- [x] Thêm rule dependency direction giữa layers/modules.
- [x] Thêm rule read/write segregation.
- [x] Cấu hình CI gate fail khi vi phạm.

### 3.5 API Hardening & Configuration Modularization
- [x] Tách JWT & Auth Policies ra `AuthenticationExtensions`.
- [x] Tách EF Core, HealthChecks, CORS ra `InfrastructureExtensions`.
- [x] Tách MediatR, Repositories, Services ra `ApplicationExtensions`.
- [x] Tách logic Migration/Seed ra `StartupTasksExtensions`.
- [x] Làm gọn `Program.cs` thành dạng gọi Extension methods.

#### 3.5 Mandatory reference checklist (phải đọc trước khi làm)
- [ ] `Favi-BE.API/Program.cs`
- [ ] `Favi-BE.API/Authorization/*`
- [ ] `Favi-BE.API/HealthChecks/*`
- [ ] `Favi-BE.API/Data/*`
- [ ] `Favi-BE.API/Services/*`
- [ ] `Favi-BE.API/Interfaces/*`
- [ ] `Favi-BE.BuildingBlocks/Infrastructure/Pipeline/*`

### Exit criteria Slice 0
- [x] Build pass.
- [x] Baseline tests pass.
- [x] Outbox write atomic cùng aggregate state.
- [x] Inbox idempotent under replay.

---

## 4) Slice 1 — `Auth.LoginCQRS`

### 4.1 Command migration (ưu tiên Login)
- [x] Khởi tạo project `Favi-BE.Modules.Auth` (.csproj) tại thư mục gốc.
- [x] Thêm project vào Solution và tham chiếu từ `Favi-BE.API`.
- [x] `LoginCommand` + handler (viết trong `Favi-BE.Modules.Auth`).
- [x] `RefreshTokenCommand` + handler.
- [x] `LogoutCommand` + handler.
- [x] `RegisterCommand` + handler (nếu local auth còn dùng).
- [x] `ChangePasswordCommand` + handler.
- [ ] `RequestPasswordResetCommand` + handler. _(deferred: email flow chưa có SMTP setup)_
- [ ] `ResetPasswordCommand` + handler. _(deferred: phụ thuộc password reset token store)_

### 4.2 Controller strangler
- [x] `AuthController` gọi `IMediator.Send(...)` thay orchestration trực tiếp.
- [x] Giữ nguyên response contract cho client (AuthResponse, error codes).
- [x] Giữ nguyên policy/authorization behavior.
- [x] Thêm endpoints mới: `POST /logout`, `POST /change-password`, `GET /me`.

### 4.2.1 Module port + adapter pattern
- [x] `IAuthWriteRepository` (port) + `AuthWriteRepositoryAdapter` (adapter trong API).
- [x] `IJwtTokenService` (port) + `JwtTokenServiceAdapter` (adapter trong API).
- [x] `IAuthQueryReader` (port) + `AuthQueryReaderAdapter` (adapter dùng AsNoTracking).
- [x] `AddAuthModule()` extension method trong `AuthModuleExtensions.cs`.
- [x] `ApplicationExtensions` gọi `AddAuthModule()`.

### 4.3 Session/token lifecycle
- [ ] Chuẩn hóa lưu refresh token/session (AuthSession table — deferred, additive migration). _(deferred: AuthSession table additive migration)_
- [ ] Xử lý revoke + expire + audit cơ bản. _(deferred: phụ thuộc AuthSession)_

### Exit criteria Slice 1
- [x] Build pass (0 errors).
- [x] Auth parity confirmed: Login/Register/Refresh giữ nguyên response contract.
- [x] Token issuance format không đổi với client.
- [x] Không phát sinh lỗi bảo mật/regression (password hash BCrypt, không trả hash trong DTO).
- [x] `IAuthService` legacy còn đăng ký (additive — không xóa legacy path sớm).
- [ ] `RequestPasswordReset`/`ResetPassword` deferred — documented as decision log.

---

## 5) Slice 2 — `Notification.EventDriven`

### 5.1 Loại side-effect khỏi write transaction
- [x] Khởi tạo project `Favi-BE.Modules.Notifications` (.csproj) tại thư mục gốc.
- [x] Gỡ SignalR push trực tiếp khỏi write path trong `Favi-BE.API` (DI switched to `OutboxNotificationService`; legacy `NotificationService` kept as rollback fallback).
- [x] Tạo domain events cho reaction/comment/follow (`CommentCreatedIntegrationEvent`, `UserFollowedIntegrationEvent`, `PostReactionToggledIntegrationEvent`, `CommentReactionToggledIntegrationEvent`).

### 5.2 Event-driven flow
- [x] In-process handlers chỉ cập nhật cùng module (`OutboxNotificationService` chỉ ghi OutboxMessage, không side-effect ngoài).
- [x] Cross-boundary events vào outbox (`OutboxNotificationService.EnqueueOutboxAsync` → `IOutbox.AddAsync` + `SaveChangesAsync`).
- [x] Outbox processor publish notification event (`OutboxProcessor` cập nhật: dispatch theo `IInboxConsumer.MessageType`).
- [x] Notification handler thực hiện persistence/read-model update + SignalR push (`CommentCreatedNotificationConsumer`, `UserFollowedNotificationConsumer`, `PostReactionToggledNotificationConsumer`, `CommentReactionToggledNotificationConsumer` — mỗi consumer: idempotent via `IInbox`, persist `Notification`, push SignalR qua `INotificationRealtimeGateway`).

### 5.3 Contract compatibility
- [x] Giữ nguyên event name client: `ReceiveNotification`, `UnreadCountUpdated` (hardcoded trong `NotificationRealtimeGatewayAdapter`).
- [x] Chặn duplicate sends (idempotency guard: `IInbox.TryStartProcessingAsync(messageId, consumerName, ...)` mỗi consumer).

### Exit criteria Slice 2
- [x] Không còn hub push trong transaction write (`IHubContext` không còn được gọi trong CREATE notification path; chỉ còn trong `MarkAllAsReadAsync` — không thuộc write transaction).
- [ ] Có automated test (integration/architecture) chứng minh không có SignalR push trong transactional write path.
- [x] Không duplicate notification (inbox dedup theo `messageId + consumerName`).
- [x] Unread count parity đạt yêu cầu (`GetUnreadCountAsync` vẫn query trực tiếp từ DB sau khi consumer persist notification).

---

## 6) Slice 3 — `Engagement.Commands`

- [x] `CreateCommentCommand`.
- [x] `TogglePostReactionCommand`.
- [x] `ToggleCommentReactionCommand`.
- [x] `ToggleCollectionReactionCommand`.
- [x] `ToggleRepostReactionCommand`.
- [x] Áp dụng domain rules + idempotency.
- [x] Bổ sung integration events cần thiết cho Notifications.

### Exit criteria Slice 3
- [x] Reaction/comment correctness parity.
- [x] Idempotency checks pass.

---

## 7) Slice 4 — `SocialGraph.Commands`

- [ ] `FollowUserCommand`.
- [ ] `UnfollowUserCommand`.
- [ ] `AddSocialLinkCommand`.
- [ ] `RemoveSocialLinkCommand`.
- [ ] Tách cross-module interaction qua integration events.

### Exit criteria Slice 4
- [ ] Follow graph parity.
- [ ] Notification parity cho follow/unfollow.

---

## 8) Slice 5 — `ContentPublishing.Commands`

- [ ] `CreatePostCommand`, `UpdatePostCommand`, `DeletePostCommand`, `ArchivePostCommand`.
- [ ] Nhóm media/tag commands (`AddPostMedia`, `ReorderPostMedia`, `AddPostTags`, `RemovePostTag`).
- [ ] Nhóm collection commands: `CreateCollectionCommand`, `UpdateCollectionCommand`, `DeleteCollectionCommand`, `AddPostToCollectionCommand`, `RemovePostFromCollectionCommand`.
- [ ] Nhóm repost commands: `SharePostCommand`, `UnsharePostCommand`.
- [ ] Đảm bảo transaction boundary trong aggregate.

### Exit criteria Slice 5
- [ ] Content mutation parity.
- [ ] Không regression ở media/tag paths.

---

## 9) Slice 6 — `Stories.CommandsAndExpiry`

- [ ] `CreateStoryCommand`, `ArchiveStoryCommand`, `DeleteStoryCommand`.
- [ ] `RecordStoryViewCommand`.
- [ ] `ExpireStoryCommand` (internal/background).
- [ ] Căn chỉnh background expiration + cleanup.

### Exit criteria Slice 6
- [ ] Expiration reliability parity.
- [ ] Media cleanup parity.

---

## 10) Slice 7 — `Messaging.CQRS`

- [ ] Tách read/write cho conversation/message.
- [ ] `GetOrCreateDmCommand`, `CreateGroupConversationCommand`, `SendMessageCommand`, `MarkConversationReadCommand`.
- [ ] Query handlers dùng read contracts `AsNoTracking`.
- [ ] Realtime hooks chỉ đi qua notification/event pipeline phù hợp.

### Exit criteria Slice 7
- [ ] Latency/error SLO parity.
- [ ] Message-read correctness parity.

---

## 11) Slice 8 — `Moderation.BackofficeCQRS`

- [ ] `CreateReportCommand`, `ResolveReportCommand`.
- [ ] `ModerateUserCommand`, `RevokeModerationCommand`.
- [ ] `LogAdminActionCommand`.
- [ ] Queries audit/report/history qua read side tách biệt.

### Exit criteria Slice 8
- [ ] Audit integrity parity.
- [ ] Admin workflow parity.

---

## 12) Read/Write segregation rollout checklist

### 13.1 Contract level
- [ ] Mỗi module có `I<Module>CommandRepository`, `I<Module>UnitOfWork`, `I<Module>QueryReader`.

### 13.2 Handler rules
- [ ] Command handlers dùng write DbContext (tracking).
- [ ] Command handlers không gọi `I<Module>QueryReader`.
- [ ] Query handlers không thực hiện mutate/tracking writes.

### 13.3 Query migration seam
- [ ] Mỗi query có mapping: `EFCoreAsNoTracking` hiện tại -> ứng viên `Dapper` tương lai.
- [ ] Hot queries được đánh dấu để chuyển projection/read model trước.

---

## 13) Schema transition checklist (Expand -> Migrate -> Switch -> Contract)

### 14.1 Expand
- [ ] Migration additive cho outbox/inbox/auth sessions/read projections cần thiết.

### 14.2 Migrate/Backfill
- [ ] Backfill dữ liệu với script an toàn, idempotent.
- [ ] Đối chiếu record counts + integrity.

### 14.3 Switch
- [ ] Chuyển query handlers sang read contracts mới.
- [ ] Theo dõi metrics so sánh old/new.

### 14.4 Contract
- [ ] Chỉ xóa cột/bảng legacy sau khi parity + ổn định được xác nhận.

---

## 14) Boundary enforcement checklist

- [ ] Mỗi module có facade interface rõ ràng.
- [ ] API chỉ phụ thuộc facade/module contracts.
- [ ] Cấm cross-module internals trực tiếp.
- [ ] Architecture tests cover dependency matrix.
- [ ] CI fail-fast khi vi phạm boundary.

---

## 15) Validation checklist bắt buộc cho mỗi slice

- [ ] Build passes.
- [ ] Tests pass (unit/integration tương ứng).
- [ ] Backward compatibility validated.
- [ ] Migration scripts validated trên dữ liệu staging-like.
- [ ] Outbox/Inbox idempotency scenarios validated.
- [ ] Observability logs/metrics/traces cập nhật.
- [ ] Rollback test (`git revert`) đã chạy thử.
- [ ] Architecture boundary tests pass.
- [ ] Documentation updated đồng bộ với implementation.

---

## 16) Mốc bàn giao theo nhịp release

- [ ] R0: Discovery + tài liệu kiến trúc hoàn chỉnh.
- [ ] R1: Foundation.CQRSOutbox merged.
- [x] R2: Auth.LoginCQRS merged.
- [x] R3: Notification.EventDriven merged.
- [ ] R4: Engagement + SocialGraph merged.
- [ ] R5: ContentPublishing.Commands merged.
- [ ] R6: Stories.CommandsAndExpiry merged.
- [ ] R7: Messaging.CQRS merged.
- [ ] R8: Moderation.BackofficeCQRS merged.
- [ ] R9: Hardening + cleanup + final parity signoff.

---

## 17) Definition of Done toàn chương trình

- [ ] Tất cả slice bắt buộc đã merge theo đúng thứ tự.
- [ ] Không còn SignalR push trong transactional write path.
- [ ] Outbox/Inbox chạy ổn định, không mất event, không duplicate side effects.
- [ ] Module boundaries được enforce tự động bằng tests + CI.
- [ ] CQRS read/write segregation được áp dụng nhất quán.
- [ ] Bộ 16 tài liệu deliverables hoàn thiện end-to-end.
- [ ] Có hồ sơ rollback và parity report cho từng slice.

---

## 18) Parity report artifact bắt buộc cho mỗi slice

- [ ] Có báo cáo so sánh `old vs new command result parity` cho từng slice.
- [ ] Có báo cáo so sánh `key read-model metrics parity` cho từng slice.
- [ ] Báo cáo được lưu cùng release notes/mốc merge của slice tương ứng.

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
- [] Schema transition: đã đọc `Docs/CQRS-Modernization/Schema-Transition-Plan.md` + `Docs/CQRS-Modernization/Outbox-Implementation-Guide.md` + `Docs/CQRS-Modernization/Inbox-Implementation-Guide.md`.
- [ ] Boundary enforcement: đã đọc `Docs/CQRS-Modernization/Module-Boundary-Enforcement.md` + `Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md`.

#### 2.y.2 Theo slice triển khai
- [x] `Foundation.CQRSOutbox`: đã đọc `BuildingBlocks-Design.md` + `Outbox-Implementation-Guide.md` + `Inbox-Implementation-Guide.md` + `Schema-Transition-Plan.md`.
- [x] `Auth.LoginCQRS`: đã đọc `Auth-CQRS-Catalog.md` + `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md`.
- [x] `Notification.EventDriven`: đã đọc `Notification-Refactor-SignalR-MediatR.md` + `Outbox-Implementation-Guide.md` + `Inbox-Implementation-Guide.md`.
- [x] `Engagement.Commands`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Favi-Concrete-Module-Aggregate-Matrix.md`.
- [x] `SocialGraph.Commands`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Architecture-BoundedContexts.md` + `Favi-Concrete-Module-Aggregate-Matrix.md`.
- [x] `ContentPublishing.Commands`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Schema-Transition-Plan.md`.
- [x] `Stories.CommandsAndExpiry`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Schema-Transition-Plan.md`.
- [x] `Messaging.CQRS`: đã đọc `CQRS-CommandQuery-Catalog.md` + `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` + `Module-Boundary-Enforcement.md`.
- [x] `Moderation.BackofficeCQRS`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Aggregate-Inventory.md` + `Module-Boundary-Enforcement.md`.
- [x] `Stories.Queries`: đã đọc `CQRS-CommandQuery-Catalog.md` + `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` + `Module-Boundary-Enforcement.md`.
- [ ] `ContentDiscovery.Queries`: đã đọc `CQRS-CommandQuery-Catalog.md` + `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` + `Favi-Concrete-Module-Aggregate-Matrix.md` + `Architecture-BoundedContexts.md`.
- [ ] `Notifications.CommandsAndQueries`: đã đọc `CQRS-CommandQuery-Catalog.md` + `Notification-Refactor-SignalR-MediatR.md` + `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`.
- [ ] `Auth.ProfileQueries`: đã đọc `Auth-CQRS-Catalog.md` + `CQRS-CommandQuery-Catalog.md` + `Favi-Concrete-Module-Aggregate-Matrix.md`.
- [ ] `Integration.DomainEvents`: đã đọc `Notification-Refactor-SignalR-MediatR.md` + `Outbox-Implementation-Guide.md` + `BuildingBlocks-Design.md`.
- [ ] `ArchTests.ReadWriteSegregation`: đã đọc `Module-Boundary-Enforcement.md` + `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`.
- [ ] `Domain.AggregateRoots`: đã đọc `Aggregate-Inventory.md` + `Architecture-BoundedContexts.md` + `BuildingBlocks-Design.md`.
- [ ] `Facade.ModuleContracts`: đã đọc `Module-Boundary-Enforcement.md` + `CQRS-CommandQuery-Catalog.md`.
- [ ] `Infrastructure.ModuleDbContexts` (optional): đã đọc `Architecture-BoundedContexts.md` + `Schema-Transition-Plan.md` + `Module-Boundary-Enforcement.md`.

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

- [x] `FollowUserCommand`.
- [x] `UnfollowUserCommand`.
- [x] `AddSocialLinkCommand`.
- [x] `RemoveSocialLinkCommand`.
- [x] Tách cross-module interaction qua integration events.

### Exit criteria Slice 4
- [x] Follow graph parity.
- [x] Notification parity cho follow/unfollow.

---

## 8) Slice 5 — `ContentPublishing.Commands`

- [x] `CreatePostCommand`, `UpdatePostCommand`, `DeletePostCommand`, `ArchivePostCommand`.
- [x] Nhóm media/tag commands (`AddPostMedia`, `ReorderPostMedia`, `AddPostTags`, `RemovePostTag`).
- [x] Nhóm collection commands: `CreateCollectionCommand`, `UpdateCollectionCommand`, `DeleteCollectionCommand`, `AddPostToCollectionCommand`, `RemovePostFromCollectionCommand`.
- [x] Nhóm repost commands: `SharePostCommand`, `UnsharePostCommand`.
- [x] Đảm bảo transaction boundary trong aggregate (1 SaveAsync per command, NSFW/Vector tách ra ngoài transaction).

### Exit criteria Slice 5
- [x] Content mutation parity.
- [x] Không regression ở media/tag paths.

---

## 9) Slice 6 — `Stories.CommandsAndExpiry`

- [x] `CreateStoryCommand`, `ArchiveStoryCommand`, `DeleteStoryCommand`.
- [x] `RecordStoryViewCommand`.
- [x] `ExpireStoryCommand` (internal/background).
- [x] Căn chỉnh background expiration + cleanup.

### Exit criteria Slice 6
- [x] Expiration reliability parity.
- [x] Media cleanup parity.

---

## 10) Slice 7 — `Messaging.CQRS`

- [x] Tách read/write cho conversation/message.
- [x] `GetOrCreateDmCommand`, `CreateGroupConversationCommand`, `SendMessageCommand`, `MarkConversationReadCommand`.
- [x] Query handlers dùng read contracts `AsNoTracking`.
- [x] Realtime hooks chỉ đi qua notification/event pipeline phù hợp.

### Exit criteria Slice 7
- [x] Latency/error SLO parity.
- [x] Message-read correctness parity.

---

## 11) Slice 8 — `Moderation.BackofficeCQRS`

- [x] `CreateReportCommand`, `ResolveReportCommand`.
- [x] `ModerateUserCommand`, `RevokeModerationCommand`.
- [x] `LogAdminActionCommand`.
- [x] Queries audit/report/history qua read side tách biệt.

### Exit criteria Slice 8
- [x] Audit integrity parity.
- [x] Admin workflow parity.

---

## 12) Slice 9 — `Stories.Queries`

### 12.1 Query port + read models
- [x] `IStoriesQueryReader` (port trong `Favi-BE.Modules.Stories/Application/Contracts`).
- [x] `StoryReadModel` (ReadModel trong `Contracts/ReadModels`).
- [x] `StoryViewerReadModel` (ReadModel trong `Contracts/ReadModels`).

### 12.2 Query handlers
- [x] `GetStoryByIdQuery` + `GetStoryByIdQueryHandler`.
- [x] `GetActiveStoriesByProfileQuery` + `GetActiveStoriesByProfileQueryHandler` — handler absorbs `IProfileService.GetEntityByIdAsync(profileId)` guard (validate profile exists internally, không cần inject IProfileService vào controller).
- [x] `GetViewableStoriesQuery` + `GetViewableStoriesQueryHandler`.
- [x] `GetArchivedStoriesQuery` + `GetArchivedStoriesQueryHandler`.
- [x] `GetStoryViewersQuery` + `GetStoryViewersQueryHandler`.
- [x] `GetActiveStoryCountQuery` + `GetActiveStoryCountQueryHandler`.

### 12.3 Adapter
- [x] `StoriesQueryReaderAdapter` trong `Favi-BE.API/Application/Stories` (inject `IPrivacyGuard` để giữ parity privacy logic với legacy `StoryService`).
- [x] `AddStoriesModule()` cập nhật để đăng ký `IStoriesQueryReader`.

### 12.4 Controller strangler
- [x] `StoriesController`: `GET /stories/{id}` — thay `_stories.GetByIdAsync` bằng `_mediator.Send(new GetStoryByIdQuery(...))`.
- [x] `StoriesController`: `GET /stories/profile/{profileId}` — thay `_stories.GetActiveStoriesByProfileAsync` bằng `_mediator.Send(new GetActiveStoriesByProfileQuery(...))`.
- [x] `StoriesController`: `GET /stories/feed` — thay `_stories.GetViewableStoriesAsync` bằng `_mediator.Send(new GetViewableStoriesQuery(...))`.
- [x] `StoriesController`: `GET /stories/archived` — thay `_stories.GetArchivedStoriesAsync` bằng `_mediator.Send(new GetArchivedStoriesQuery(...))`.
- [x] `StoriesController`: `GET /stories/{id}/viewers` — thay `_stories.GetViewersAsync` bằng `_mediator.Send(new GetStoryViewersQuery(...))`.
- [x] `StoriesController`: `GET /stories/profile/{profileId}/count` — thay `_stories.GetActiveStoryCountAsync` bằng `_mediator.Send(new GetActiveStoryCountQuery(...))`.
- [x] `StoriesController`: `POST /stories` (Create) — thay reload `_stories.GetByIdAsync` bằng `_mediator.Send(new GetStoryByIdQuery(...))`.
- [x] `StoriesController`: bỏ inject `IStoryService` khỏi constructor.

### 12.5 Architecture tests
- [x] `StoriesModuleArchitectureTests`: thêm STR-01..STR-06 — CQRS segregation (CMD/QRY isolation, WriteModels, handler internals) + cross-module boundary enforcement. 53/53 tests pass.

### Exit criteria Slice 9
- [x] Build pass (0 errors, 0 warnings mới).
- [x] `IStoryService` không còn trong `StoriesController` constructor.
- [x] `IProfileService` không còn trong `StoriesController` constructor (guard absorbed vào `GetActiveStoriesByProfileQueryHandler`).
- [x] Architecture tests pass (STR-01..STR-06, 53/53 total).
- [ ] Read parity: kết quả giống legacy `IStoryService` calls cho tất cả 6 endpoints.

---

## 13) Slice 10 — `ContentDiscovery.Queries`

> **Architectural decision (2026-05-10):** Tách read side ra khỏi ContentPublishing. `Favi-BE.Modules.ContentPublishing` chỉ giữ write path (commands). Toàn bộ query/read path (feed, post detail, collection listing, repost listing) chuyển sang module mới `Favi-BE.Modules.ContentDiscovery` — pure read context, không có aggregates, không có write operations.

### 13.0 Khởi tạo module ContentDiscovery
- [x] Tạo project `Favi-BE.Modules.ContentDiscovery` (.csproj) tại thư mục gốc.
- [x] Thêm project vào Solution và tham chiếu từ `Favi-BE.API`.
- [x] Tạo `AssemblyReference.cs`.

### 13.1 Query port + read models
> Tất cả read models đặt trong `Favi-BE.Modules.ContentDiscovery/Application/Contracts/ReadModels/`.

- [x] `IContentDiscoveryQueryReader` (port trong `Favi-BE.Modules.ContentDiscovery/Application/Contracts`).
- [x] `PostReadModel` — fields thuần content: Id, AuthorProfileId, Caption, CreatedAt, UpdatedAt, Privacy, Medias, Tags, Location, IsNSFW, CommentsCount. Không chứa reaction data (thuộc Engagement).
- [x] `PostMediaReadModel`.
- [x] `TagReadModel`.
- [x] `PostLocationReadModel`.
- [x] `CollectionReadModel` — fields thuần content: Id, OwnerProfileId, Title, Description, CoverImageUrl, Privacy, CreatedAt, UpdatedAt, PostIds, PostCount. Không chứa reaction data.
- [x] `RepostReadModel` — fields thuần content + RepostsCount + IsRepostedByCurrentUser. Không chứa reaction data.
- [x] `FeedItemReadModel` — Kind (Post/Repost), Post?, Repost?, CreatedAt.

### 13.2 Query handlers — Post/Feed/Repost
> Tất cả handlers đặt trong `Favi-BE.Modules.ContentDiscovery/Application/Queries/`.

- [x] `GetPostByIdQuery` + `GetPostByIdQueryHandler`.
- [x] `GetNewsFeedQuery` + `GetNewsFeedQueryHandler`.
- [x] `GetGuestFeedQuery` + `GetGuestFeedQueryHandler`.
- [x] `GetExploreFeedQuery` + `GetExploreFeedQueryHandler`.
- [x] `GetLatestFeedQuery` + `GetLatestFeedQueryHandler`.
- [x] `GetProfilePostsQuery` + `GetProfilePostsQueryHandler` — absorbs profile-exists guard, không inject IProfileService vào controller.
- [x] `SearchPostsQuery` + `SearchPostsQueryHandler`.
- [x] `GetArchivedPostsQuery` + `GetArchivedPostsQueryHandler`.
- [x] `GetRecycleBinQuery` + `GetRecycleBinQueryHandler`.
- [x] `GetRepostsByProfileQuery` + `GetRepostsByProfileQueryHandler`.
- [x] `GetRepostByIdQuery` + `GetRepostByIdQueryHandler`.
- [x] `GetFeedWithRepostsQuery` + `GetFeedWithRepostsQueryHandler`.

### 13.3 Query handlers — Collection
- [x] `GetCollectionByIdQuery` + `GetCollectionByIdQueryHandler`.
- [x] `GetCollectionsQuery` + `GetCollectionsQueryHandler` (GET /collections/owner/{ownerId}).
- [x] `GetCollectionPostsQuery` + `GetCollectionPostsQueryHandler` (GET /collections/{id}/posts).
- [x] `GetTrendingCollectionsQuery` + `GetTrendingCollectionsQueryHandler` (GET /collections/trending).

### 13.4 Adapter
- [x] `ContentDiscoveryQueryReaderAdapter` trong `Favi-BE.API/Application/ContentDiscovery`.
  - Inject: `IUnitOfWork`, `IPrivacyGuard`, `IEngagementQueryReader` (để lấy reaction summary cho PostResponse assembly tại controller).
  - Trả về `PostReadModel` không có reactions — controller gọi `GetPostReactionsQuery` riêng khi cần assemble `PostResponse` đầy đủ.
- [x] `AddContentDiscoveryModule()` extension method trong `Favi-BE.API/DependencyInjection/ContentDiscoveryModuleExtensions.cs`.
- [x] `ApplicationExtensions` gọi `AddContentDiscoveryModule()`.

### 13.5 Controller strangler — PostController
> **Lưu ý**: Slice 5 chỉ implement command handlers, PostController CHƯA được strangled cho command calls. Slice 10 thay cả command lẫn query calls.
- [x] `PostController`: thay tất cả write calls (`_posts.CreateAsync`, `UpdateAsync`, `DeleteAsync`, `RestoreAsync`, `PermanentDeleteAsync`, `ArchiveAsync`, `UnarchiveAsync`, `SharePostAsync`, `UnsharePostAsync`, `UploadMediaAsync`) bằng `_mediator.Send(...)` tương ứng (ContentPublishing commands).
- [x] `PostController`: thay tất cả read calls (`_posts.GetByIdAsync`, `GetByProfileAsync`, `GetFeedAsync`, `GetFeedWithRepostsAsync`, `GetGuestFeedAsync`, `GetExploreAsync`, `GetLatestAsync`, `GetRecycleBinAsync`, `GetArchivedAsync`, `GetRepostsByProfileAsync`, `GetRepostAsync`, `GetEntityAsync`) bằng `_mediator.Send(...)` tương ứng (ContentDiscovery queries). GetEntityAsync absorbed vào query handlers.
- [x] `PostController`: `GetByProfile` action — guard `_profileService.GetEntityByIdAsync(profileId)` absorbed vào `GetProfilePostsQueryHandler`; bỏ inject `IProfileService` khỏi `PostController` constructor.
- [x] `PostController`: khi map `PostReadModel` → `PostResponse`, gọi thêm `_mediator.Send(new GetPostReactionsQuery(...))` từ Engagement để lấy reaction summary.
- [x] `PostController`: bỏ inject `IPostService` khỏi constructor.

### 13.6 Controller strangler — CommentsController
> Chỉ còn 1 `_posts.GetEntityAsync(postId)` guard tại `GetCommentsByPost`.
- [x] `CommentsController`: `GetCommentsByPost` action — thay `_posts.GetEntityAsync(postId)` guard bằng `_mediator.Send(new GetPostByIdQuery(postId, viewerId))` từ ContentDiscovery (trả `null` → 404).
- [x] `CommentsController`: bỏ inject `IPostService` khỏi constructor.

### 13.7 Controller strangler — CollectionsController
> CollectionsController chưa được strangled dù command handlers đã có từ Slice 5.
- [x] `CollectionsController`: thay write calls (`_collections.CreateAsync`, `UpdateAsync`, `DeleteAsync`, `AddPostAsync`, `RemovePostAsync`) bằng `_mediator.Send(...)` tương ứng (ContentPublishing commands).
- [x] `CollectionsController`: thay read calls (`_collections.GetByIdAsync`, `GetByOwnerAsync`, `GetPostsAsync`, `GetTrendingCollectionsAsync`, `GetEntityByIdAsync`) bằng `_mediator.Send(...)` tương ứng (ContentDiscovery queries). GetEntityByIdAsync absorbed vào query handlers.
- [x] `CollectionsController`: `GetReactorsAsync` — thay bằng `_mediator.Send(new GetCollectionReactorsQuery(...))` từ Engagement (đã có từ Slice 3).
- [x] `CollectionsController`: khi map `CollectionReadModel` → `CollectionResponse`, gọi thêm `_mediator.Send(new GetCollectionReactionsQuery(...))` từ Engagement.
- [x] `CollectionsController`: bỏ inject `ICollectionService` khỏi constructor.

### 13.8 Architecture tests
- [x] `ContentDiscoveryModuleArchitectureTests`: test `QueryHandlers_Should_Not_Depend_On_ContentPublishing_Internals` (CD-01..CD-05, 5 tests).
- [x] `ContentPublishingModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace` already present as CP-01 (passes — no Queries namespace in ContentPublishing post-split).

### Exit criteria Slice 10
- [x] Build pass (0 errors, 0 warnings mới).
- [x] `IPostService` không còn trong `PostController` constructor.
- [x] `IProfileService` không còn trong `PostController` constructor.
- [x] `IPostService` không còn trong `CommentsController` constructor.
- [x] `ICollectionService` không còn trong `CollectionsController` constructor.
- [x] Architecture tests pass (59/59, including CD-01..CD-05).
- [ ] Read parity cho tất cả feed/post/collection/repost/search endpoints.

---

## 14) Slice 11 — `Notifications.CommandsAndQueries`

### 14.1 Query port + read model
- [ ] `INotificationQueryReader` (port trong `Favi-BE.Modules.Notifications/Application/Contracts`).
- [ ] `NotificationReadModel` (ReadModel trong `Contracts/ReadModels`).

### 14.2 Query handlers
- [ ] `GetNotificationsQuery` + `GetNotificationsQueryHandler`.
- [ ] `GetUnreadNotificationCountQuery` + `GetUnreadNotificationCountQueryHandler`.

### 14.3 Command handlers
- [ ] `MarkNotificationAsReadCommand` + `MarkNotificationAsReadCommandHandler`.
- [ ] `MarkAllNotificationsAsReadCommand` + `MarkAllNotificationsAsReadCommandHandler`.
- [ ] `DeleteNotificationCommand` + `DeleteNotificationCommandHandler`.

### 14.4 Adapters
- [ ] `NotificationQueryReaderAdapter` trong `Favi-BE.API/Application/Notifications` (dùng AsNoTracking).
- [ ] `NotificationCommandRepositoryAdapter` trong `Favi-BE.API/Application/Notifications`.
- [ ] `AddNotificationsModule()` cập nhật để đăng ký `INotificationQueryReader` + command repository.

### 14.5 Controller strangler
- [ ] `NotificationsController`: `GET /notifications` — thay `_notifications.GetNotificationsAsync` bằng `_mediator.Send(new GetNotificationsQuery(...))`.
- [ ] `NotificationsController`: `GET /notifications/unread-count` — thay `_notifications.GetUnreadCountAsync` bằng `_mediator.Send(new GetUnreadNotificationCountQuery(...))`.
- [ ] `NotificationsController`: `PUT /notifications/{id}/read` — thay `_notifications.MarkAsReadAsync` bằng `_mediator.Send(new MarkNotificationAsReadCommand(...))`.
- [ ] `NotificationsController`: `PUT /notifications/read-all` — thay `_notifications.MarkAllAsReadAsync` bằng `_mediator.Send(new MarkAllNotificationsAsReadCommand(...))`.
- [ ] `NotificationsController`: `DELETE /notifications/{id}` — thay `_notifications.DeleteNotificationAsync` bằng `_mediator.Send(new DeleteNotificationCommand(...))`.
- [ ] `NotificationsController`: bỏ inject `INotificationService` khỏi constructor.

### 14.6 Architecture tests
- [ ] `NotificationsModuleArchitectureTests`: thêm test `CommandHandlers_Should_Not_Depend_On_Queries_Namespace`.

### Exit criteria Slice 11
- [ ] Build pass (0 errors, 0 warnings mới).
- [ ] `INotificationService` không còn trong `NotificationsController` constructor.
- [ ] Architecture tests pass.
- [ ] Read/write parity cho tất cả 5 notification endpoints.

---

## 15) Slice 12 — `Auth.ProfileQueries`

### 15.1 Query handlers
- [ ] `GetProfileByIdQuery` + `GetProfileByIdQueryHandler` (theo `Auth-CQRS-Catalog.md`).
- [ ] `GetRecommendedProfilesQuery` + `GetRecommendedProfilesQueryHandler`.
- [ ] `GetOnlineFriendsQuery` + `GetOnlineFriendsQueryHandler`.
- [ ] `GetProfileAvatarQuery` + `GetProfileAvatarQueryHandler` (GET /profiles/avatar/{profileId}).
- [ ] `GetProfilePosterQuery` + `GetProfilePosterQueryHandler` (GET /profiles/poster/{profileId}).

### 15.2 Command handlers
- [ ] `UpdateProfileCommand` + `UpdateProfileCommandHandler`.
- [ ] `DeleteProfileCommand` + `DeleteProfileCommandHandler`.
- [ ] `UploadAvatarCommand` + `UploadAvatarCommandHandler` — file bytes resolved bởi API layer adapter trước khi dispatch; handler nhận stream/URL, không nhận `IFormFile`.
- [ ] `UploadPosterCommand` + `UploadPosterCommandHandler` — tương tự `UploadAvatarCommand`.
- [ ] `SyncProfileCommand` + `SyncProfileCommandHandler` — idempotent upsert từ Supabase webhook: no-op nếu profile đã tồn tại, create nếu chưa có.
- [ ] `UpdateLastActiveCommand` — **đã implement trong Auth module** (ChatController đang dùng); chỉ cần gọi `_mediator.Send(new UpdateLastActiveCommand(userId))` trong ProfilesController heartbeat endpoint.

### 15.3 Adapter
- [ ] Bổ sung `GetProfileByIdAsync`, `GetRecommendedAsync`, `GetOnlineFriendsAsync`, `GetAvatarAsync`, `GetPosterAsync` vào `AuthQueryReaderAdapter`.
- [ ] `AddAuthModule()` cập nhật để đăng ký các handler mới.

### 15.4 Controller strangler
> **Lưu ý**: SocialGraph commands (Follow/Unfollow/AddLink/RemoveLink) và queries (GetFollowers/GetFollowings/GetSocialLinks) đã được strangled từ Slice 4 — chỉ cần thay các `_profiles.*` calls còn lại.
- [ ] `ProfilesController`: `GET /profiles/{id}` — thay `_profiles.GetByIdAsync` + `GetEntityByIdAsync` guard bằng `_mediator.Send(new GetProfileByIdQuery(...))`.
- [ ] `ProfilesController`: `PUT /profiles` — thay `_profiles.UpdateAsync` bằng `_mediator.Send(new UpdateProfileCommand(...))`.
- [ ] `ProfilesController`: `DELETE /profiles` — thay `_profiles.DeleteAsync` bằng `_mediator.Send(new DeleteProfileCommand(...))`.
- [ ] `ProfilesController`: `GET /profiles/avatar/{profileId}` — thay `_profiles.GetAvatar` bằng `_mediator.Send(new GetProfileAvatarQuery(...))`.
- [ ] `ProfilesController`: `GET /profiles/poster/{profileId}` — thay `_profiles.GetPoster` bằng `_mediator.Send(new GetProfilePosterQuery(...))`.
- [ ] `ProfilesController`: `POST /profiles/avatar` — thay `_profiles.UploadAvatarAsync` bằng `_mediator.Send(new UploadAvatarCommand(...))` (resolve file trước, send command với stream/URL).
- [ ] `ProfilesController`: `POST /profiles/poster` — thay `_profiles.UploadPosterAsync` bằng `_mediator.Send(new UploadPosterCommand(...))`.
- [ ] `ProfilesController`: `GET /profiles/recommendations` — thay `_profiles.GetRecommendedAsync` + `GetEntityByIdAsync` guard bằng `_mediator.Send(new GetRecommendedProfilesQuery(...))`.
- [ ] `ProfilesController`: `GET /profiles/online-friends` — thay `_profiles.GetOnlineFriendsAsync` + `GetEntityByIdAsync` guard bằng `_mediator.Send(new GetOnlineFriendsQuery(...))`.
- [ ] `ProfilesController`: `POST /profiles/heartbeat` — thay `_profiles.UpdateLastActiveAsync` bằng `_mediator.Send(new UpdateLastActiveCommand(userId))` (handler đã có trong Auth module).
- [ ] Guard calls `_profiles.GetEntityByIdAsync(...)` rải rác (Follow, Followers, Followings, UploadAvatar, UploadPoster, heartbeat) — absorbed vào từng query/command handler; xóa khỏi controller sau khi strangled.
- [ ] `ProfilesController`: bỏ inject `IProfileService` khỏi constructor.

### 15.5 Controller strangler — ProfileSyncController
> `ProfilesSyncController` là Supabase auth webhook (`POST /api/ProfilesSync/sync`, AllowAnonymous). Không có mediator hiện tại.
- [ ] `ProfilesSyncController`: thay `_profiles.GetByIdAsync(dto.user_id)` bằng `_mediator.Send(new GetProfileByIdQuery(dto.user_id))`.
- [ ] `ProfilesSyncController`: thay `_profiles.CreateProfileAsync(...)` bằng `_mediator.Send(new SyncProfileCommand(dto.user_id, username, displayName))`.
- [ ] `ProfilesSyncController`: inject `IMediator`; bỏ inject `IProfileService` khỏi constructor.

### 15.7 Architecture tests
- [ ] `AuthModuleArchitectureTests`: thêm test `CommandHandlers_Should_Not_Depend_On_Queries_Namespace`.

### Exit criteria Slice 12
- [ ] Build pass (0 errors, 0 warnings mới).
- [ ] `IProfileService` không còn trong `ProfilesController` constructor.
- [ ] `IProfileService` không còn trong `ProfilesSyncController` constructor.
- [ ] Architecture tests pass.
- [ ] Read parity cho profile/avatar/poster/recommended/online-friends endpoints.
- [ ] Heartbeat endpoint vẫn hoạt động qua `UpdateLastActiveCommand`.
- [ ] Supabase sync endpoint vẫn idempotent qua `SyncProfileCommand`.

---

## 16) Slice 13 — `Integration.DomainEvents`

### 16.1 Domain events trong SocialGraph module
- [ ] Tạo `UserFollowedDomainEvent` trong `Favi-BE.Modules.SocialGraph/Domain/Events`.
- [ ] `FollowUserCommandHandler`: raise `UserFollowedDomainEvent` trên aggregate thay vì gọi `ISocialGraphNotificationService`.
- [ ] Bỏ inject `ISocialGraphNotificationService` khỏi `FollowUserCommandHandler`.

### 16.2 Domain events trong Engagement module
- [ ] Tạo `CommentCreatedDomainEvent` trong `Favi-BE.Modules.Engagement/Domain/Events`.
- [ ] Tạo `ReactionToggledDomainEvent` trong `Favi-BE.Modules.Engagement/Domain/Events`.
- [ ] `CreateCommentCommandHandler`: raise `CommentCreatedDomainEvent` thay vì gọi `IEngagementNotificationService`.
- [ ] `TogglePostReactionCommandHandler`: raise `ReactionToggledDomainEvent` thay vì gọi `IEngagementNotificationService`.
- [ ] `ToggleCommentReactionCommandHandler`: raise `ReactionToggledDomainEvent` thay vì gọi `IEngagementNotificationService`.
- [ ] Bỏ inject `IEngagementNotificationService` khỏi tất cả handlers.

### 16.3 Domain notifications mappers
- [ ] `SocialGraphDomainNotificationsMapper` trong `Favi-BE.API/Application/SocialGraph`: map `UserFollowedDomainEvent` → `UserFollowedIntegrationEvent` → outbox.
- [ ] `EngagementDomainNotificationsMapper` trong `Favi-BE.API/Application/Engagement`: map `CommentCreatedDomainEvent` → `CommentCreatedIntegrationEvent`, `ReactionToggledDomainEvent` → `PostReactionToggledIntegrationEvent` hoặc `CommentReactionToggledIntegrationEvent` tùy target.
- [ ] Đăng ký mappers vào `IDomainNotificationsMapper` pipeline trong DI.

### 16.4 Cleanup (sau khi parity validated)
- [ ] Xóa `ISocialGraphNotificationService` + `SocialGraphNotificationServiceAdapter`.
- [ ] Xóa `IEngagementNotificationService` + `EngagementNotificationServiceAdapter`.

### Exit criteria Slice 13
- [ ] Build pass (0 errors, 0 warnings mới).
- [ ] `ISocialGraphNotificationService` và `IEngagementNotificationService` không còn trong handlers.
- [ ] `TransactionBehavior` dispatch domain events → outbox trong cùng transaction.
- [ ] Notification parity: follow/comment/reaction notifications vẫn hoạt động đúng qua domain event path.
- [ ] Architecture tests pass.
- [ ] `SocialGraphNotificationServiceAdapter` và `EngagementNotificationServiceAdapter` đã bị xóa.

---

## 17) Slice 14 — `ArchTests.ReadWriteSegregation`

### 17.1 CQRS segregation tests cho tất cả modules có đủ Commands + Queries
- [ ] `StoriesModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace` (sau Slice 9).
- [ ] `ContentPublishingModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace` (sau Slice 10 — ContentPublishing không còn Queries namespace sau khi tách ContentDiscovery).
- [ ] `ContentDiscoveryModuleArchitectureTests`: `QueryHandlers_Should_Not_Depend_On_ContentPublishing_Internals` (sau Slice 10).
- [ ] `NotificationsModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace` (sau Slice 11).
- [ ] `AuthModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace` (sau Slice 12).
- [ ] `EngagementModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace`.
- [ ] `SocialGraphModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace`.
- [ ] `ModerationModuleArchitectureTests`: `CommandHandlers_Should_Not_Depend_On_Queries_Namespace`.

### 17.2 Controller boundary tests
- [ ] `ApiLayerArchitectureTests`: Controllers không inject `IStoryService` trực tiếp (sau Slice 9).
- [ ] `ApiLayerArchitectureTests`: Controllers không inject `IPostService` trực tiếp — covers `PostController` + `CommentsController` (sau Slice 10).
- [ ] `ApiLayerArchitectureTests`: Controllers không inject `ICollectionService` trực tiếp (sau Slice 10).
- [ ] `ApiLayerArchitectureTests`: Controllers không inject `INotificationService` trực tiếp (sau Slice 11).
- [ ] `ApiLayerArchitectureTests`: Controllers không inject `IProfileService` trực tiếp — covers `ProfilesController` + `PostController` + `StoriesController` + `ProfilesSyncController` (sau Slice 12).

### 17.3 Handler isolation tests
- [ ] Architecture test: không handler nào inject `INotificationService` trực tiếp (sau Slice 13).
- [ ] Architecture test: không handler nào inject `ISocialGraphNotificationService` hoặc `IEngagementNotificationService` (sau Slice 13).

### Exit criteria Slice 14
- [ ] Tất cả architecture tests pass.
- [ ] CI gate fail-fast khi vi phạm boundary.

---

## 18) Slice 15 — `Domain.AggregateRoots`

Mục tiêu: Business logic và invariants rời khỏi command handlers vào aggregate domain model. Handler chỉ còn orchestration (load aggregate → gọi domain method → save).

> **Prerequisite:** BuildingBlocks đã có `AggregateRoot<TId>`, `IBusinessRule`, `BusinessRuleValidationException` — không cần thêm gì ở tầng này.

### 18.1 Identity & Access
- [ ] `Profile` aggregate root — domain methods: `Update()`, `Ban()`, `Unban()`, `Delete()`.
- [ ] `EmailAccount` aggregate root — domain methods: `ChangePassword()`, `ValidateCredentials()`.
- [ ] Business rules: `UsernameUniquenessRule`, `PasswordHashRequiredRule`.
- [ ] Refactor `UpdateProfileCommandHandler`, `ChangePasswordCommandHandler` dùng aggregate methods.

### 18.2 Social Graph
- [ ] `FollowRelationship` aggregate root — domain methods: `Create()`, `Remove()`.
- [ ] Business rules: `CannotSelfFollowRule`, `DuplicateFollowRule`.
- [ ] `SocialLink` aggregate root — domain methods: `Add()`, `Remove()`.
- [ ] Business rules: `DuplicateProviderPerProfileRule`.
- [ ] Refactor `FollowUserCommandHandler`, `UnfollowUserCommandHandler`, `AddSocialLinkCommandHandler`, `RemoveSocialLinkCommandHandler`.

### 18.3 Content Publishing
- [ ] `Post` aggregate root — domain methods: `Create()`, `Update()`, `Archive()`, `Delete()`, `Restore()`, `AddMedia()`, `ReorderMedia()`, `AddTag()`, `RemoveTag()`.
- [ ] `Collection` aggregate root — domain methods: `Create()`, `Update()`, `Delete()`, `AddPost()`, `RemovePost()`.
- [ ] `Repost` aggregate root — domain methods: `Share()`, `Unshare()`.
- [ ] Business rules: `OwnerOnlyMutateRule`, `UniqueMediaPositionRule`, `DuplicateRepostRule`.
- [ ] Refactor tất cả ContentPublishing command handlers.

### 18.4 Engagement
- [ ] `CommentThread` aggregate root — domain methods: `AddComment()`, `UpdateComment()`, `DeleteComment()`.
- [ ] `Reaction` aggregate root — domain method: `Toggle()`.
- [ ] Business rules: `ValidParentCommentRule`, `OneReactionPerUserPerTargetRule`.
- [ ] Refactor tất cả Engagement command handlers.

### 18.5 Notifications
- [ ] `Notification` aggregate root — domain methods: `Create()`, `MarkAsRead()`, `Delete()`.
- [ ] Business rules: `NoSelfNotificationRule`.
- [ ] Refactor notification command handlers + inbox consumers.

### 18.6 Stories
- [ ] `Story` aggregate root — domain methods: `Create()`, `Archive()`, `Delete()`, `Expire()`, `RecordView()`.
- [ ] Business rules: `StoryTTL24hRule`, `NoDuplicateViewRule`, `OwnerOnlyArchiveRule`.
- [ ] Refactor tất cả Stories command handlers.

### 18.7 Messaging
- [ ] `Conversation` aggregate root — domain methods: `CreateDm()`, `CreateGroup()`.
- [ ] `Message` aggregate root — domain methods: `Send()`, `MarkRead()`.
- [ ] Business rules: `DmUniquenessRule`, `SenderMustBeMemberRule`, `MonotonicReadMarkerRule`.
- [ ] Refactor tất cả Messaging command handlers.

### 18.8 Moderation & Trust
- [ ] `ReportCase` aggregate root — domain methods: `Create()`, `Resolve()`.
- [ ] `UserModeration` aggregate root — domain methods: `Moderate()`, `Revoke()`.
- [ ] `AdminAction` aggregate root — domain method: `Log()` (immutable — không có mutate method).
- [ ] Business rules: `ValidReporterTargetRule`, `ReportLifecycleOpenToResolvedRule`, `BanWindowConsistencyRule`.
- [ ] Refactor tất cả Moderation command handlers.

### 18.9 Architecture tests
- [ ] Convention: command handlers không còn chứa `if (...) throw` business rule logic trực tiếp — chỉ gọi aggregate domain method + `CheckRule(...)`. Enforce bằng code review gate hoặc Roslyn analyzer nếu có.

### Exit criteria Slice 15
- [ ] Build pass (0 errors, 0 warnings mới).
- [ ] Tất cả invariants implement dưới dạng `IBusinessRule` và throw `BusinessRuleValidationException`.
- [ ] Command handlers không còn chứa business conditionals trực tiếp — chỉ orchestrate.
- [ ] Behavior parity với trước refactor cho tất cả command endpoints.

---

## 19) Slice 16 — `Facade.ModuleContracts`

Mục tiêu: API layer không reference module-internal command/query types trực tiếp. Controller inject module facade thay vì `IMediator` với command/query types cụ thể.

### 19.1 Facade interface definition

| Facade | Location | Phạm vi method |
|---|---|---|
| `IAuthFacade` | `Favi-BE.Modules.Auth/Application/IAuthFacade.cs` | Login, Register, Logout, RefreshToken, ChangePassword, GetProfile, UpdateProfile, DeleteProfile, UploadAvatar, UploadPoster, GetRecommended, GetOnlineFriends, SyncProfile, UpdateLastActive, GetProfileAvatar, GetProfilePoster |
| `ISocialGraphFacade` | `Favi-BE.Modules.SocialGraph/Application/ISocialGraphFacade.cs` | Follow, Unfollow, AddSocialLink, RemoveSocialLink, GetFollowers, GetFollowings, GetSocialLinks |
| `IContentPublishingFacade` | `Favi-BE.Modules.ContentPublishing/Application/IContentPublishingFacade.cs` | CreatePost, UpdatePost, DeletePost, ArchivePost, RestorePost, PermanentDeletePost, UploadMedia, AddPostTags, RemovePostTag, ReorderPostMedia, CreateCollection, UpdateCollection, DeleteCollection, AddPostToCollection, RemovePostFromCollection, SharePost, UnsharePost |
| `IContentDiscoveryFacade` | `Favi-BE.Modules.ContentDiscovery/Application/IContentDiscoveryFacade.cs` | GetPostById, GetNewsFeed, GetFeedWithReposts, GetGuestFeed, GetExploreFeed, GetLatestFeed, GetProfilePosts, GetArchivedPosts, GetRecycleBin, SearchPosts, GetRepostById, GetRepostsByProfile, GetCollectionById, GetCollections, GetCollectionPosts, GetTrendingCollections |
| `IEngagementFacade` | `Favi-BE.Modules.Engagement/Application/IEngagementFacade.cs` | CreateComment, ToggleReaction variants + comment/reaction queries |
| `INotificationsFacade` | `Favi-BE.Modules.Notifications/Application/INotificationsFacade.cs` | GetNotifications, GetUnreadCount, MarkAsRead, MarkAllAsRead, DeleteNotification |
| `IStoriesFacade` | `Favi-BE.Modules.Stories/Application/IStoriesFacade.cs` | CreateStory, ArchiveStory, DeleteStory, RecordView + all query methods |
| `IMessagingFacade` | `Favi-BE.Modules.Messaging/Application/IMessagingFacade.cs` | GetOrCreateDm, CreateGroup, SendMessage, MarkRead + query methods |
| `IModerationFacade` | `Favi-BE.Modules.Moderation/Application/IModerationFacade.cs` | CreateReport, ResolveReport, ModerateUser, RevokeModeration, LogAdminAction + query methods |

### 19.2 Facade implementation
- [ ] Mỗi module có `<Module>Facade : I<Module>Facade` trong `Application/` — delegate mọi call sang `IMediator.Send(...)` tương ứng.
- [ ] `Add<Module>Module()` extension đăng ký `I<Module>Facade → <Module>Facade` (Scoped).

### 19.3 Controller strangler — second pass
- [ ] Mỗi controller: thay inject `IMediator` bằng inject `I<Module>Facade`.
- [ ] Thay `_mediator.Send(new XCommand(...))` bằng `_facade.DoX(...)`.
- [ ] Xóa `using Favi_BE.Modules.*.Application.Commands` và `using Favi_BE.Modules.*.Application.Queries` khỏi controller files.

### 19.4 Cross-module facade — formalize adapter contracts
- [ ] Rà soát tất cả `I*Adapter` ports hiện tại — upgrade/rename thành `I<Module>Facade` cross-module contracts nếu chưa align với naming convention.
- [ ] Đảm bảo module-to-module calls không reference sibling module internal namespaces.

### 19.5 Architecture tests
- [ ] `ApiLayerArchitectureTests`: API assembly không có dependency vào `*.Commands.*` hoặc `*.Queries.*` namespace của bất kỳ module nào.
- [ ] `ApiLayerArchitectureTests`: Controllers không inject `IMediator` trực tiếp sau khi facade hoàn chỉnh.

### Exit criteria Slice 16
- [ ] Build pass (0 errors, 0 warnings mới).
- [ ] Controllers không còn reference internal module command/query types.
- [ ] Architecture tests enforce API → Facade boundary.
- [ ] Feature parity toàn bộ endpoints.

---

## 20) Slice 17 (Optional) — `Infrastructure.ModuleDbContexts`

> **Scope:** Optional — ưu tiên thấp. Triển khai khi cần isolation mạnh hơn hoặc chuẩn bị tách microservice. Không bắt buộc cho modular monolith đang hoạt động. Thực hiện sau khi Slice 15 (aggregate domain model) hoàn chỉnh vì cần đảm bảo không còn cross-module EF navigation properties.

Mục tiêu: Mỗi module có DbContext riêng, chỉ map các bảng thuộc ownership của module đó. Tất cả vẫn dùng cùng 1 physical database.

### 20.1 Shared infra DbContext
- [ ] Tạo `FaviInfraDbContext` chứa `OutboxMessages`, `InboxMessages` — tách khỏi module DbContexts.
- [ ] Migration history của infra DbContext là nguồn chính.

### 20.2 Per-module DbContext
- [ ] `AuthDbContext` — owns: `profiles`, `email_accounts`, `auth_sessions`.
- [ ] `SocialGraphDbContext` — owns: `follows`, `social_links`.
- [ ] `ContentPublishingDbContext` — owns: `posts`, `post_media`, `post_tags`, `collections`, `post_collections`, `reposts`, `tags`.
- [ ] `EngagementDbContext` — owns: `comments`, `reactions`.
- [ ] `NotificationsDbContext` — owns: `notifications`.
- [ ] `StoriesDbContext` — owns: `stories`, `story_views`.
- [ ] `MessagingDbContext` — owns: `conversations`, `messages`, `user_conversations`, `message_reads`.
- [ ] `ModerationDbContext` — owns: `reports`, `user_moderations`, `admin_actions`.

### 20.3 Migration split
- [ ] Split migration hiện tại thành per-module migration files — validate thứ tự áp dụng.
- [ ] Xác nhận không còn cross-module EF navigation properties (chỉ còn ID references giữa modules).
- [ ] `dotnet ef` chạy được cho từng module DbContext độc lập.

### 20.4 Cross-module join resolution
- [ ] Audit tất cả query handlers dùng cross-module navigation → chuyển sang ID-based lookup hoặc application-level join.
- [ ] Không query nào join cross-module tables qua EF navigation properties.

### 20.5 Adapter updates + AppDbContext removal
- [ ] Mỗi module adapter inject đúng module DbContext thay vì `AppDbContext`.
- [ ] `AppDbContext` deprecated và xóa sau khi tất cả module DbContexts hoàn chỉnh.

### Exit criteria Slice 17
- [ ] Build pass.
- [ ] `AppDbContext` đã xóa hoàn toàn.
- [ ] Mỗi module DbContext chỉ chứa entities thuộc ownership của module đó.
- [ ] Migrations chạy được per-module.
- [ ] Feature parity toàn bộ endpoints.

---

## 21) Read/Write segregation rollout checklist

### 21.1 Contract level
- [ ] Mỗi module có `I<Module>CommandRepository`, `I<Module>UnitOfWork`, `I<Module>QueryReader`.

### 21.2 Handler rules
- [ ] Command handlers dùng write DbContext (tracking).
- [ ] Command handlers không gọi `I<Module>QueryReader`.
- [ ] Query handlers không thực hiện mutate/tracking writes.

### 21.3 Query migration seam
- [ ] Mỗi query có mapping: `EFCoreAsNoTracking` hiện tại -> ứng viên `Dapper` tương lai.
- [ ] Hot queries được đánh dấu để chuyển projection/read model trước.

---

## 22) Schema transition checklist (Expand -> Migrate -> Switch -> Contract)

### 22.1 Expand
- [ ] Migration additive cho outbox/inbox/auth sessions/read projections cần thiết.

### 22.2 Migrate/Backfill
- [ ] Backfill dữ liệu với script an toàn, idempotent.
- [ ] Đối chiếu record counts + integrity.

### 22.3 Switch
- [ ] Chuyển query handlers sang read contracts mới.
- [ ] Theo dõi metrics so sánh old/new.

### 22.4 Contract
- [ ] Chỉ xóa cột/bảng legacy sau khi parity + ổn định được xác nhận.

---

## 23) Boundary enforcement checklist

- [ ] Mỗi module có facade interface rõ ràng.
- [ ] API chỉ phụ thuộc facade/module contracts.
- [ ] Cấm cross-module internals trực tiếp.
- [ ] Architecture tests cover dependency matrix.
- [ ] CI fail-fast khi vi phạm boundary.

---

## 24) Validation checklist bắt buộc cho mỗi slice

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

## 25) Mốc bàn giao theo nhịp release

- [ ] R0: Discovery + tài liệu kiến trúc hoàn chỉnh.
- [x] R1: Foundation.CQRSOutbox merged.
- [x] R2: Auth.LoginCQRS merged.
- [x] R3: Notification.EventDriven merged.
- [x] R4: Engagement + SocialGraph merged.
- [x] R5: ContentPublishing.Commands merged.
- [x] R6: Stories.CommandsAndExpiry merged.
- [x] R7: Messaging.CQRS merged.
- [x] R8: Moderation.BackofficeCQRS merged.
- [x] R9: Stories.Queries merged.
- [x] R10: ContentPublishing.Queries merged.
- [ ] R11: Notifications.CommandsAndQueries merged.
- [ ] R12: Auth.ProfileQueries merged.
- [ ] R13: Integration.DomainEvents merged.
- [ ] R14: ArchTests.ReadWriteSegregation merged.
- [ ] R15: Domain.AggregateRoots merged.
- [ ] R16: Facade.ModuleContracts merged.
- [ ] R17 (optional): Infrastructure.ModuleDbContexts merged.
- [ ] R18: Hardening + cleanup + final parity signoff.

---

## 26) Definition of Done toàn chương trình

- [ ] Tất cả slice bắt buộc đã merge theo đúng thứ tự (R1–R16).
- [ ] Tất cả business invariants sống trong aggregate domain model (`IBusinessRule` + `BusinessRuleValidationException`) — không còn `if (...) throw` trực tiếp trong command handlers.
- [ ] API layer không reference module-internal command/query types — toàn bộ controller chỉ inject `I<Module>Facade`.
- [ ] Architecture tests enforce API → Facade boundary và Facade → Handler boundary.
- [ ] Không còn SignalR push trong transactional write path.
- [ ] Outbox/Inbox chạy ổn định, không mất event, không duplicate side effects.
- [ ] Module boundaries được enforce tự động bằng tests + CI.
- [ ] CQRS read/write segregation được áp dụng nhất quán trên tất cả modules.
- [ ] Không còn `IPostService` trong `PostController`, `CommentsController` constructor.
- [ ] Không còn `ICollectionService` trong `CollectionsController` constructor.
- [ ] Không còn `IStoryService`, `IProfileService` trong `StoriesController` constructor.
- [ ] Không còn `INotificationService` trong `NotificationsController` constructor.
- [ ] Không còn `IProfileService` trong `ProfilesController`, `PostController`, `ProfilesSyncController` constructor.
- [ ] Không còn `ISocialGraphNotificationService`, `IEngagementNotificationService` trong handlers.
- [ ] Bộ tài liệu deliverables hoàn thiện end-to-end.
- [ ] Có hồ sơ rollback và parity report cho từng slice.

---

## 27) Parity report artifact bắt buộc cho mỗi slice

- [ ] Có báo cáo so sánh `old vs new command result parity` cho từng slice.
- [ ] Có báo cáo so sánh `key read-model metrics parity` cho từng slice.
- [ ] Báo cáo được lưu cùng release notes/mốc merge của slice tương ứng.

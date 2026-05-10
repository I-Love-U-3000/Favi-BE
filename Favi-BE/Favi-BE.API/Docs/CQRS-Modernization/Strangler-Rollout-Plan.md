# Strangler Rollout Plan

## 1. Rollout mode
Binary per-slice: `implement -> validate runnable state -> merge`.
Rollback: `git revert` về stable commit gần nhất.

---

## 2. Ordered slices

| Slice | Scope | Status | Success metrics | Exit criteria | Rollback trigger |
|---|---|---|---|---|---|
| `Foundation.CQRSOutbox` | BuildingBlocks, MediatR, Outbox/Inbox, architecture tests | ✅ Done | Build/test baseline, outbox write rate | Build pass + tests pass + atomic outbox | build/test fail, lost event |
| `Auth.LoginCQRS` | Login/Refresh/Logout/Register/ChangePassword handlers + AuthController strangler | ✅ Done | Auth success rate parity | Token format parity + no security regression | Login failures spike |
| `Notification.EventDriven` | Remove direct SignalR push from write transaction; route via Outbox + IInboxConsumer | ✅ Done | Duplicate send = 0 | No in-transaction hub push + unread parity | Duplicate sends/unread mismatch |
| `Engagement.Commands` | Comment/reaction command handlers + CommentsController strangler | ✅ Done | Reaction parity, idempotency | Parity checks pass | Mismatch/duplicates |
| `SocialGraph.Commands` | Follow/Unfollow/SocialLink command handlers + query handlers (Followers/Followings/SocialLinks) + ProfilesController partial strangler | ✅ Done | Graph parity | Follow graph and notification parity | Follow mismatch |
| `ContentPublishing.Commands` | Post/Collection/Repost write command handlers | ✅ Done | Write success parity | Content mutation parity | Data mismatch |
| `Stories.CommandsAndExpiry` | Story command handlers + background expiry via MediatR | ✅ Done | Expiry reliability | Parity for expire/cleanup | Stale stories leak |
| `Messaging.CQRS` | Conversation/message command + query handlers + ChatController full strangler | ✅ Done | Latency/error parity | Message-read correctness parity | Message ordering/read errors |
| `Moderation.BackofficeCQRS` | Report/moderation/admin command + query handlers + ReportsController + AdminReportsController + AdminUsersController + AdminAuditController strangler | ✅ Done | Audit consistency | Audit/admin parity | Audit integrity issue |
| `Stories.Queries` | Story query handlers (GetStoryById, GetActiveStoriesByProfile, GetViewableStories, GetArchivedStories, GetStoryViewers, GetActiveStoryCount) + StoriesController full strangler | ✅ Done (manually ticked this) | Read parity for all story endpoints | `IStoryService` removed from `StoriesController` constructor | Read result mismatch |
| `ContentPublishing.Queries` | Post/feed/collection/repost/search query handlers (GetPostById, GetNewsFeed, GetGuestFeed, GetExploreFeed, GetLatestFeed, GetProfilePosts, SearchPosts, GetArchivedPosts, GetRecycleBin, GetRepostsByProfile, GetRepostById, GetCollectionById, GetCollections) + PostController full strangler | ⏳ Pending | Read parity for all post/feed/collection/repost endpoints | `IPostService` removed from `PostController` constructor | Read result mismatch |
| `Notifications.CommandsAndQueries` | Notification command handlers (MarkAsRead, MarkAllAsRead, Delete) + query handlers (GetNotifications, GetUnreadNotificationCount) + NotificationsController full strangler | ⏳ Pending | Read/write parity for all notification endpoints | `INotificationService` removed from `NotificationsController` constructor | Notification count mismatch |
| `Auth.ProfileQueries` | Profile query handlers (GetProfileById, GetRecommendedProfiles, GetOnlineFriends) + UpdateProfileCommand + ProfilesController full strangler | ⏳ Pending | Read parity for all profile endpoints | `IProfileService` removed from `ProfilesController` constructor | Profile read mismatch |
| `Integration.DomainEvents` | Replace `ISocialGraphNotificationService`/`IEngagementNotificationService` adapter calls in handlers with proper domain events raised on aggregates; wire `IDomainNotificationsMapper` per module | ⏳ Pending | Notification parity via domain event path | No handler injects `ISocialGraphNotificationService` or `IEngagementNotificationService`; `TransactionBehavior` dispatches domain events to outbox in same transaction | Notification loss or duplicate |
| `ArchTests.ReadWriteSegregation` | CQRS segregation tests for all modules (CommandHandlers not referencing Queries namespace); controller boundary tests (no legacy service injection); handler isolation tests (no direct notification service injection) | ⏳ Pending | All architecture tests pass | CI gate fail-fast on any boundary violation | Any test failure |
| `Domain.AggregateRoots` | Aggregate root classes + `IBusinessRule` implementations per module; refactor all command handlers to: load aggregate → call domain method → save | ⏳ Pending | Business invariants enforced by domain model | No `if (...) throw` business logic in command handlers; all invariants via `IBusinessRule` + `BusinessRuleValidationException` | Behavior regression in any command endpoint |
| `Facade.ModuleContracts` | `I<Module>Facade` per module wrapping `IMediator`; controller strangler second pass replaces `IMediator` inject; architecture tests enforce API → Facade boundary | ⏳ Pending | API layer isolation from module internals | API assembly has no direct reference to module command/query types; controllers inject only module facades | Build/test fail |
| `Infrastructure.ModuleDbContexts` *(optional)* | Per-module DbContext owning only its tables; shared infra DbContext for Outbox/Inbox; migration split; cross-module EF navigation removal; `AppDbContext` removal | ⏳ Optional | Module DB isolation | `AppDbContext` removed; each module DbContext verified; migrations run per-module | Build/test fail or migration error |

---

## 3. Mandatory validations each slice
- Build/test pass.
- Backward compatibility validated.
- Outbox/Inbox idempotency validated.
- Architecture boundary tests pass.
- Documentation updated.

---

## 4. Mandatory parity artifacts each slice
- `old vs new command result parity`.
- `key read-model metrics parity`.

---

## 5. Mandatory reference files per slice

| Slice | Priority reference files |
|---|---|
| `Foundation.CQRSOutbox` | `BuildingBlocks-Design.md`, `Outbox-Implementation-Guide.md`, `Inbox-Implementation-Guide.md`, `Schema-Transition-Plan.md` |
| `Auth.LoginCQRS` | `Auth-CQRS-Catalog.md`, `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md` |
| `Notification.EventDriven` | `Notification-Refactor-SignalR-MediatR.md`, `Outbox-Implementation-Guide.md`, `Inbox-Implementation-Guide.md` |
| `Engagement.Commands` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md`, `Favi-Concrete-Module-Aggregate-Matrix.md` |
| `SocialGraph.Commands` | `CQRS-CommandQuery-Catalog.md`, `Architecture-BoundedContexts.md`, `Favi-Concrete-Module-Aggregate-Matrix.md` |
| `ContentPublishing.Commands` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md`, `Schema-Transition-Plan.md` |
| `Stories.CommandsAndExpiry` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md`, `Schema-Transition-Plan.md` |
| `Messaging.CQRS` | `CQRS-CommandQuery-Catalog.md`, `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`, `Module-Boundary-Enforcement.md` |
| `Moderation.BackofficeCQRS` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md`, `Module-Boundary-Enforcement.md` |
| `Stories.Queries` | `CQRS-CommandQuery-Catalog.md`, `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`, `Module-Boundary-Enforcement.md` |
| `ContentPublishing.Queries` | `CQRS-CommandQuery-Catalog.md`, `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`, `Favi-Concrete-Module-Aggregate-Matrix.md` |
| `Notifications.CommandsAndQueries` | `CQRS-CommandQuery-Catalog.md`, `Notification-Refactor-SignalR-MediatR.md`, `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` |
| `Auth.ProfileQueries` | `Auth-CQRS-Catalog.md`, `CQRS-CommandQuery-Catalog.md`, `Favi-Concrete-Module-Aggregate-Matrix.md` |
| `Integration.DomainEvents` | `Notification-Refactor-SignalR-MediatR.md`, `Outbox-Implementation-Guide.md`, `BuildingBlocks-Design.md` |
| `ArchTests.ReadWriteSegregation` | `Module-Boundary-Enforcement.md`, `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` |
| `Domain.AggregateRoots` | `Aggregate-Inventory.md`, `Architecture-BoundedContexts.md`, `BuildingBlocks-Design.md` |
| `Facade.ModuleContracts` | `Module-Boundary-Enforcement.md`, `CQRS-CommandQuery-Catalog.md` |
| `Infrastructure.ModuleDbContexts` (optional) | `Architecture-BoundedContexts.md`, `Schema-Transition-Plan.md`, `Module-Boundary-Enforcement.md` |

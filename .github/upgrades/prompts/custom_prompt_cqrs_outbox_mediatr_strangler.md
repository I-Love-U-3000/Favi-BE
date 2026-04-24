# `custom_prompt_cqrs_outbox_mediatr_strangler` — Custom Upgrade Execution Prompt

## 0) Context (auto-filled)
- **Scenario Name**: `CQRS + Outbox + MediatR + Strangler Migration Plan`
- **Solution Path**: `C:\Users\tophu\source\repos\Favi-BE\Favi-BE\Favi-BE.sln`
- **Project Path (if scoped)**: ``
- **Target Framework**: `.NET 9` (architecture-focused migration; no TFM uplift required)
- **Repository Root**: `C:\Users\tophu\source\repos\Favi-BE`
- **Current Git Branch**: `straightforward-modular-monolith`
- **Git Working Tree Status**: `Dirty (pending changes detected: ?? ../nul)`

> **Execution note**: Preserve current uncommitted work. Do not overwrite unrelated changes. Prefer incremental commits per module and per strangler slice.

---

## 1) Mission and Non-Goals
You are executing an **incremental architecture modernization** from a modular monolith toward:
- **CQRS** (separate write/read models and handlers)
- **MediatR** (request/notification pipeline)
- **Transactional Outbox + Inbox** (reliable publish/consume with idempotency)
- **SignalR real-time notifications integrated through MediatR notifications**
- **Strangler Pattern rollout** (feature-by-feature replacement, zero big-bang)
- **BuildingBlocks shared kernel** (stable cross-module abstractions)

### Non-Goals
- No full microservices split in this phase.
- No full rewrite of all modules at once.
- No schema breaking changes without backward-compatible transition.

---

## 2) Required Outcomes
Produce and then execute (phase-by-phase) a complete plan that includes:
1. **Business module decomposition** (bounded contexts/domain boundaries).
2. **Aggregate root inventory** (count + ownership + invariants).
3. **Command/Query catalog per aggregate/module**.
4. **Auth command set** with explicit `LoginCommand` and token/session lifecycle.
5. **Domain boundary mapping** (what stays internal vs integration events).
6. **Schema redesign plan** (write model, read model projections, outbox/inbox tables, transition strategy).
7. **Folder restructuring blueprint** (target clean architecture layout + migration mapping from current folders).
8. **BuildingBlocks shared kernel design** (`Domain`, `Application`, `Infrastructure` abstractions).
9. **Outbox + Inbox implementation design** and sample implementation path.
10. **Read/Write interface segregation strategy** (EF Core tracking for write, EF Core `AsNoTracking` for read, Dapper-ready read abstraction).
11. **MediatR adoption path** with pipeline behaviors.
12. **Notification refactor path** for SignalR + `INotification`/`INotificationHandler`.
13. **Module boundary enforcement plan** (facade interfaces + dependency rule tests).
14. **Phased strangler migration roadmap** with concrete ordered slices (what first, what next, why), milestones, risk controls, rollback points, and acceptance criteria.
15. **Optional InternalCommands decision** (adopt now or defer with rationale).
16. **Concrete Favi module/aggregate baseline** mapped from existing entities/services/controllers.

---

## 3) Discovery and Baseline Steps (must run first)
1. Analyze the full solution and gather module/entity/repository/service dependencies.
2. Build dependency map of current modules (API, application/service, domain/entity, infrastructure/repositories, SignalR hubs).
3. Identify existing transaction boundaries (`UnitOfWork`, repository commits, side effects such as SignalR calls).
4. Identify notification-related flow end-to-end (HTTP entrypoint -> service -> persistence -> SignalR push).
5. Identify candidate business domains from existing entities and use cases.
6. Detect cross-module direct calls that bypass integration events.
7. Validate build baseline before architecture changes.

Deliverables for this section:
- Current-state architecture map.
- Current anti-pattern list (mixed read/write concerns, side effects inside transactions, direct infrastructure coupling, cross-module direct calls, no idempotent consumer).

STOP: Section 19 must be validated. Discovery must confirm or correct the module/aggregate list before starting any implementation.

---

## 4) Target Architecture Definition
Define target architecture using these slices:
- **BuildingBlocks**
  - `BuildingBlocks.Domain` (`Entity`, `ValueObject`, `IBusinessRule`, `IDomainEvent`, `TypedIdValueBase`)
  - `BuildingBlocks.Application` (`IExecutionContextAccessor`, `IDomainEventNotification<T>`, outbox/inbox contracts)
  - `BuildingBlocks.Infrastructure` (domain event dispatching, outbox/inbox processors, optional internal commands)
- **Modules** (`<Module>.Domain`, `<Module>.Application`, `<Module>.Infrastructure`, `<Module>.API`/presentation adapters)
- **Presentation** (`Favi-BE.API`, hubs, controllers) depending only on module facade interfaces.

### Mandates
- All use-cases go through MediatR handlers.
- Dependency Injection must be modularized via `IServiceCollection` extension methods (no raw registrations in `Program.cs`); group by responsibility (for example `AddInfrastructure`, `AddAuthModule`).
- Domain invariants live in domain model via `IBusinessRule`; application validation only handles input/contract shape.
- Domain events are collected from tracked aggregates and dispatched in-process before outbox enqueue.
- Outbox is for cross-boundary delivery; Inbox is for idempotent consumption.
- No direct cross-module application/domain calls; use module facade + integration events.
- Writes use dedicated command repositories/UnitOfWork with EF Core tracking.
- Reads use dedicated query readers with EF Core `AsNoTracking`; keep query contracts storage-agnostic for future Dapper replacement.
- SignalR push is triggered from notification handlers, never from transactional write path.
- DbContext strategy: one shared DbContext for all modules, partitioned by table prefixes (`posts_`, `notifications_`, `auth_`, ...).
- Outbox and Inbox tables must live in the same shared DbContext to guarantee atomic writes.

---

## 5) Domain Decomposition Tasks
For each discovered business module:
1. Propose **bounded context name** and responsibility.
2. List entities and select **aggregate roots**.
3. Specify invariants and transactional consistency scope per aggregate.
4. Define explicit `IBusinessRule` implementations per aggregate invariant.
5. Mark cross-context interactions as integration events.
6. Count total aggregate roots and produce a matrix:
   - `AggregateRoot`
   - `Owned Entities`
   - `Business Rules`
   - `Commands`
   - `Queries`
   - `Events Published`
   - `External Dependencies`

Output format required:
- Markdown tables and diagrams (textual acceptable) with explicit counts.

---

## 6) Command/Query Catalog (CQRS)
For each aggregate root, generate:
- **Commands**: create/update/delete/state-transition operations.
- **Queries**: list/detail/search/feed/count operations.
- Handler ownership and idempotency requirements.
- **Two-layer validation strategy**:
  - **Domain validation**: `IBusinessRule` in aggregate/entity, throwing domain exception.
  - **Application validation**: FluentValidation (or equivalent) in MediatR pipeline for request shape and basic constraints.
- Authorization touchpoints.

Mandatory auth scope in catalog:
- `LoginCommand`
- `RegisterCommand` (if local auth retained)
- `RefreshTokenCommand`
- `LogoutCommand`
- `ChangePasswordCommand` / `RequestPasswordResetCommand` / `ResetPasswordCommand` (if in scope)
- `GetCurrentUserQuery`
- `GetAuthSessionsQuery` (if session tracking enabled)

Also define naming convention:
- Commands: `VerbNounCommand`
- Queries: `Get/List/Search...Query`
- Handlers: `...Handler`

---

## 7) Schema Evolution and Data Strategy
Design transition-safe schema changes:
1. Add `OutboxMessages` table with fields:
   - `Id`, `OccurredOnUtc`, `Type`, `Payload`, `CorrelationId`, `CausationId`, `Status`, `Retries`, `ProcessedOnUtc`, `Error`
2. Add `InboxMessages` table with fields:
   - `Id`, `ReceivedOnUtc`, `Type`, `Payload`, `MessageId`, `Consumer`, `Status`, `Retries`, `ProcessedOnUtc`, `Error`
3. Optionally add `InternalCommands` table if deferred/scheduled commands are required.
4. Keep existing tables operational while introducing read projections.
5. Add auth/session tables if missing (`RefreshTokens` or `AuthSessions`) with revocation and expiration fields.
6. Keep a separate read access layer contract from day 1 even if both read/write use same DB.
7. Add projection/read tables only where needed for query performance.
8. Define migration scripts in backward-compatible phases:
   - Expand (additive)
   - Migrate/backfill
   - Switch reads
   - Contract (drop deprecated columns only after validation)

Require explicit mapping per module:
- Existing table -> New ownership/read model/outbox-inbox relation.

---

## 8) Folder Restructure Plan (detailed)
Propose and apply an incremental folder structure like:
- `Favi-BE.BuildingBlocks/`
  - `Domain/`
    - `Entity.cs`, `ValueObject.cs`, `IBusinessRule.cs`, `IDomainEvent.cs`, `TypedIdValueBase.cs`
  - `Application/`
    - `IExecutionContextAccessor.cs`
    - `Events/IDomainEventNotification.cs`
    - `Outbox/IOutbox.cs`
    - `Inbox/IInbox.cs`
  - `Infrastructure/`
    - `DomainEventsDispatching/`
    - `Outbox/`
    - `Inbox/`
    - `InternalCommands/` (optional)
- `Favi-BE.Modules.<ModuleName>/`
  - `Domain/`
  - `Application/`
  - `Infrastructure/`
- `Favi-BE.API/`
  - `Controllers/...`
  - `Hubs/...`
  - `DependencyInjection/...`

For each current folder/file, provide a **move map**:
- Source path
- Target path
- Rationale
- Migration order

---

## 9) Domain Events + Outbox Dispatching Blueprint
Implement in phases:
1. Add domain event collection to base `Entity` (domain event list + clear behavior).
2. Implement `DomainEventsAccessor` reading domain events from EF Core change tracker.
3. Implement `IDomainEventNotification` mapping (`IDomainNotificationsMapper`) from domain events to application notifications.
4. Execute dispatch flow in `UnitOfWork`/transaction boundary:
   - Phase A (in-process): publish domain events via MediatR for same-module side effects.
   - Phase B (cross-boundary): map to notification contracts and persist to `OutboxMessages` in same transaction as aggregate state.
5. Ensure domain events are cleared only after successful enqueue.
6. Implement background outbox processor (`IHostedService`) with retry + poison/error capture.
7. Publish processed outbox notifications to event bus and/or MediatR integration pipeline.
8. Add observability (structured logs, metrics, trace correlation IDs).

Acceptance criteria:
- No event loss on transient failure.
- Outbox write is atomic with aggregate state changes.
- Reprocessing is safe.

Phase A handler constraints:
- ✅ Allowed: write to repositories using the same DbContext (same transaction).
- ❌ Not allowed: SignalR push, external HTTP calls, email sending.
SignalR push is only allowed from `OutboxProcessor` after `SaveChanges`.

---

## 10) Inbox and Optional InternalCommands Blueprint
1. Implement inbox consumer pipeline for external/cross-module events.
2. Store every received message with idempotency key before handler execution.
3. Execute handler only if message not previously processed.
4. Track retries, failure state, and dead-letter handling.
5. Define consumer-side exactly-once boundary and dedup strategy.
6. Evaluate `InternalCommands` pattern:
   - If needed: persist deferred commands, poll in background, execute via `IMediator.Send(...)`, include retries.
   - If not needed now: document defer decision with trigger conditions.

Acceptance criteria:
- Idempotent consume under retry/replay.
- No duplicate side effects when same message is delivered multiple times.

---

## 11) MediatR Adoption Blueprint
1. Register MediatR in DI and scan module application assemblies.
2. Introduce pipeline behaviors:
   - Validation
   - Logging
   - Performance
   - Transaction (write commands only)
3. Replace direct service orchestration in controllers with `IMediator.Send(...)` for commands and `IMediator.Send(...)` / query gateway for queries.
4. Move side effects to notification handlers.
5. Ensure behavior ordering is explicit and documented.
6. Include `AuthController` migration with `LoginCommand` as mandatory early command slice.

---

## 12) Notification Refactor (SignalR + MediatR)
Refactor current notification flow so that:
- Write command creates domain event (e.g., `PostReactedDomainEvent`).
- In-process handlers update same-module state only (no hub push inside transaction).
- Cross-boundary notification is enqueued to outbox.
- Dispatcher publishes MediatR `INotification` (e.g., `PostReactionNotificationEvent`) in processing pipeline.
- `INotificationHandler<T>` performs:
  1) notification persistence/read-model update,
  2) SignalR push (`ReceiveNotification`, `UnreadCountUpdated`).

Specific requirement for current `NotificationService`:
- Extract pure command/query responsibilities to handlers.
- Keep SignalR sending in dedicated handler/service adapter.
- Ensure no duplicated sends and no in-transaction hub push.
- Preserve existing client contract event names.

---

## 13) Module Boundary Enforcement
Define and enforce module boundaries:
1. Each module exposes one facade interface, e.g., `IModuleFacade` with command/query execution methods.
2. API/controllers depend on facades, not on module internals.
3. Cross-module interactions use integration events + outbox/inbox, not direct references.
4. Add architectural tests (e.g., NetArchTest) to enforce dependency rules.
5. Add CI gate that fails on boundary violations.

Deliverables for this section:
- Boundary dependency rule matrix.
- Architecture test suite and CI wiring plan.

---

## 14) Strangler Pattern Migration Plan (incremental)
Execute by vertical slices in this exact order:
1. **Slice 0 — Foundation**: BuildingBlocks + MediatR + Outbox/Inbox + architecture tests baseline.
2. **Slice 1 — Auth/Login first**: `AuthController` to `LoginCommand`/`RefreshTokenCommand`/`LogoutCommand`.
3. **Slice 2 — Notification flow first strangler**: reaction/comment/follow notification side effects -> outbox + handlers + SignalR.
4. **Slice 3 — Engagement write path**: `ToggleReaction` + `CreateComment` + `ToggleCommentReaction` commands.
5. **Slice 4 — Social graph**: `Follow`/`Unfollow` and profile-social updates.
6. **Slice 5 — Content core writes**: post/media/tag/repost command path.
7. **Slice 6 — Collection writes**: collection CRUD + post membership + collection reactions.
8. **Slice 7 — Stories + expiration**: story command flow and expiration/background processing alignment.
9. **Slice 8 — Messaging**: conversation/message read-write split and realtime hooks.
10. **Slice 9 — Moderation/Admin**: report/moderation/admin actions with integration events.

For each slice provide:
- Scope
- Success metrics
- Rollback plan
- Exit criteria
- Why this order (dependency/risk rationale)

Execution mode per slice:
- Implement completely -> verify it runs correctly -> merge -> move to next slice.
- Rollback is `git revert` to the previous stable commit; no traffic-percentage rollout and no feature-flag toggle are required.

---

## 15) Execution Checklist (per phase)
For every phase/slice, enforce:
- [ ] Build passes
- [ ] Tests pass (unit + integration where applicable)
- [ ] Backward compatibility validated
- [ ] Migration scripts validated on staging-like data
- [ ] Outbox/Inbox idempotency scenarios validated
- [ ] Observability dashboards updated
- [ ] Rollback tested
- [ ] Architecture boundary tests pass
- [ ] Documentation updated

---

## 16) Final Required Deliverables
Produce these artifacts:
1. `Favi-BE.API/Docs/CQRS-Modernization/Architecture-BoundedContexts.md`
2. `Favi-BE.API/Docs/CQRS-Modernization/Aggregate-Inventory.md`
3. `Favi-BE.API/Docs/CQRS-Modernization/CQRS-CommandQuery-Catalog.md`
4. `Favi-BE.API/Docs/CQRS-Modernization/Schema-Transition-Plan.md`
5. `Favi-BE.API/Docs/CQRS-Modernization/Folder-Restructure-Mapping.md`
6. `Favi-BE.API/Docs/CQRS-Modernization/BuildingBlocks-Design.md`
7. `Favi-BE.API/Docs/CQRS-Modernization/Outbox-Implementation-Guide.md`
8. `Favi-BE.API/Docs/CQRS-Modernization/Inbox-Implementation-Guide.md`
9. `Favi-BE.API/Docs/CQRS-Modernization/Notification-Refactor-SignalR-MediatR.md`
10. `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md`
11. `Favi-BE.API/Docs/CQRS-Modernization/Strangler-Rollout-Plan.md`
12. `Execution-Checklist.md`
13. `Favi-BE.API/Docs/CQRS-Modernization/InternalCommands-Decision-Log.md`
14. `Favi-BE.API/Docs/CQRS-Modernization/Auth-CQRS-Catalog.md`
15. `Favi-BE.API/Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md`
16. `Favi-BE.API/Docs/CQRS-Modernization/ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`

Each document must be complete and end-to-end (no partial numbering, no duplicate step numbering).

---

## 17) Working Constraints
- Keep commits small and reversible.
- Prefer additive changes first.
- Do not remove legacy path until parity proven.
- If unknowns are found, pause and produce decision log with options/trade-offs.

---

## 18) Start Command for Agent
Start with:
1. Current-state analysis and bounded context proposal, including direct cross-module dependency violations.
2. Concrete Favi module/aggregate inventory from existing entities/services/controllers.
3. Command/query/business-rule matrix per concrete aggregate, including explicit auth commands (`LoginCommand` first).
4. BuildingBlocks design proposal (`IBusinessRule`, domain event base, domain event notification mapping).
5. Read/write interface segregation blueprint (EF Core tracking write + EF Core `AsNoTracking` read + Dapper migration seam).
6. Notification flow spike (first business strangler slice) with in-process dispatch + outbox + inbox + SignalR design.
7. Ordered strangler roadmap with rationale for each slice.
8. Module boundary enforcement plan (facades + architecture tests).
9. Then produce phase-by-phase implementation plan with concrete task breakdown.

---

## 19) Concrete Favi Modules, Aggregates, and Command/Query Baseline (mandatory)
Use the current codebase as baseline and produce concrete module mapping below.

### 19.1 Bounded contexts and aggregate roots
| Bounded Context | Aggregate Roots | Current Entity Inputs |
|---|---|---|
| Identity & Access | `Profile`, `EmailAccount` (or `AuthAccount`), `AuthSession` (new if needed) | `Profile`, `EmailAccount`, `AuthService`, `AuthController` |
| Social Graph | `FollowRelationship` (or `Profile` if kept internal), `SocialLink` | `Follow`, `SocialLink`, `ProfileService`, `ProfilesController` |
| Content Publishing | `Post`, `Collection`, `Repost`, `TagCatalog` | `Post`, `Collection`, `Repost`, `Tag`, `PostService`, `CollectionService`, `PostController` |
| Engagement | `CommentThread`, `Reaction` | `Comment`, `Reaction`, `CommentService`, `PostService`, `CollectionService` |
| Notifications | `Notification` | `Notification`, `NotificationService`, `NotificationHub`, `NotificationsController` |
| Stories | `Story` | `Story`, `StoryView`, `StoryService`, `StoriesController`, `StoryExpirationService` |
| Messaging | `Conversation`, `Message` | `Conversation`, `Message`, `UserConversation`, `MessageRead`, `ChatService`, `ChatController` |
| Moderation & Trust | `ReportCase`, `UserModeration`, `AdminAction` | `Report`, `UserModeration`, `AdminAction`, admin controllers |

### 19.2 Mandatory concrete command/query list by aggregate
1. **Identity & Access**
   - Commands: `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`, `ChangePasswordCommand`, `UpdateProfileCommand`
   - Queries: `GetCurrentUserQuery`, `GetProfileByIdQuery`, `GetRecommendedProfilesQuery`, `GetOnlineFriendsQuery`
2. **Social Graph**
   - Commands: `FollowUserCommand`, `UnfollowUserCommand`, `AddSocialLinkCommand`, `RemoveSocialLinkCommand`
   - Queries: `GetFollowersQuery`, `GetFollowingsQuery`, `GetSocialLinksQuery`, `GetFollowSuggestionsQuery`
3. **Content Publishing**
   - Commands: `CreatePostCommand`, `UpdatePostCommand`, `DeletePostCommand`, `ArchivePostCommand`, `AddPostMediaCommand`, `ReorderPostMediaCommand`, `AddPostTagsCommand`, `RemovePostTagCommand`, `CreateCollectionCommand`, `UpdateCollectionCommand`, `DeleteCollectionCommand`, `AddPostToCollectionCommand`, `RemovePostFromCollectionCommand`, `SharePostCommand`, `UnsharePostCommand`
   - Queries: `GetPostByIdQuery`, `GetNewsFeedQuery`, `GetProfilePostsQuery`, `SearchPostsQuery`, `GetCollectionByIdQuery`, `GetCollectionsQuery`, `GetRepostsByProfileQuery`
4. **Engagement**
   - Commands: `CreateCommentCommand`, `UpdateCommentCommand`, `DeleteCommentCommand`, `TogglePostReactionCommand`, `ToggleCommentReactionCommand`, `ToggleCollectionReactionCommand`, `ToggleRepostReactionCommand`
   - Queries: `GetCommentsByPostQuery`, `GetCommentByIdQuery`, `GetPostReactionsQuery`, `GetCommentReactionsQuery`, `GetCollectionReactionsQuery`, `GetPostReactorsQuery`, `GetCommentReactorsQuery`, `GetCollectionReactorsQuery`
5. **Notifications**
   - Commands: `CreateNotificationCommand` (internal/event-driven), `MarkNotificationAsReadCommand`, `MarkAllNotificationsAsReadCommand`, `DeleteNotificationCommand`
   - Queries: `GetNotificationsQuery`, `GetUnreadNotificationCountQuery`
6. **Stories**
   - Commands: `CreateStoryCommand`, `ArchiveStoryCommand`, `DeleteStoryCommand`, `RecordStoryViewCommand`, `ExpireStoryCommand` (internal)
   - Queries: `GetStoryByIdQuery`, `GetViewableStoriesQuery`, `GetActiveStoriesByProfileQuery`, `GetArchivedStoriesQuery`, `GetStoryViewersQuery`
7. **Messaging**
   - Commands: `GetOrCreateDmCommand`, `CreateGroupConversationCommand`, `SendMessageCommand`, `MarkConversationReadCommand`, `UpdateLastActiveCommand`
   - Queries: `GetConversationsQuery`, `GetMessagesQuery`, `GetUnreadMessagesCountQuery`
8. **Moderation & Trust**
   - Commands: `CreateReportCommand`, `ResolveReportCommand`, `ModerateUserCommand`, `RevokeModerationCommand`, `LogAdminActionCommand`
   - Queries: `GetReportsQuery`, `GetReportByIdQuery`, `GetUserModerationHistoryQuery`, `GetAdminActionAuditQuery`

---

## 20) Read/Write Segregation Strategy (EF Core now, Dapper-ready later)
1. Define per-module contracts:
   - Write: `I<Module>CommandRepository`, `I<Module>UnitOfWork`
   - Read: `I<Module>QueryReader`
2. Write implementation:
   - EF Core tracking DbContext, transactional behavior, optimistic concurrency where needed.
3. Read implementation phase 1:
   - EF Core `AsNoTracking`, projection-first DTO mapping, zero domain mutation.
4. Read implementation phase 2:
   - Move hot queries to dedicated read models/materialized views when needed.
5. Read implementation phase 3:
   - Replace selected `I<Module>QueryReader` implementations from EF Core to Dapper without changing handlers/controllers.
6. Enforce coding rules:
   - Command handlers use the write DbContext for both reads and writes within the aggregate boundary.
   - Command handlers must not call `I<Module>QueryReader`.
   - Query handlers cannot use tracking writes.
   - Add architecture tests for read/write dependency direction.
7. Delivery requirement:
   - Provide a migration map: `QueryName` -> `EFCoreAsNoTracking` now -> `Dapper` target candidate.

---

## 21) Ordered Strangler Slices and Acceptance (mandatory)
1. `Foundation.CQRSOutbox`
   - Scope: BuildingBlocks, MediatR setup, Outbox/Inbox infra, architecture tests.
   - Exit: build green + baseline tests.
2. `Auth.LoginCQRS`
   - Scope: `LoginCommand`/`RefreshTokenCommand`/`LogoutCommand` migration.
   - Exit: auth parity confirmed, token issuance unchanged for clients.
3. `Notification.EventDriven`
   - Scope: replace direct notification side effects from reaction/comment/follow paths.
   - Exit: no in-transaction hub push, no duplicate sends, unread count parity.
4. `Engagement.Commands`
   - Scope: comment/reaction command handlers.
   - Exit: reaction/comment correctness parity and idempotency checks.
5. `SocialGraph.Commands`
   - Scope: follow/unfollow/social-link commands.
   - Exit: follow graph parity and notification parity.
6. `ContentPublishing.Commands`
   - Scope: post/collection/repost write operations.
   - Exit: feed/content mutation parity, no regressions in media/tag paths.
7. `Stories.CommandsAndExpiry`
   - Scope: story command flow and expiration/background processing alignment.
   - Exit: expiration reliability and media cleanup parity.
8. `Messaging.CQRS`
   - Scope: conversation/message read-write split and realtime hooks.
   - Exit: latency/error SLO parity and message-read correctness.
9. `Moderation.BackofficeCQRS`
   - Scope: report/admin/moderation paths.
   - Exit: audit integrity parity and admin workflow parity.

For every slice:
- Execution flow: implement -> validate runnable state -> merge -> continue to next slice.
- Rollback: `git revert` to previous stable commit.
- Mandatory rollback trigger: error rate delta > agreed threshold, duplicated side effects, data mismatch.
- Mandatory comparison: old vs new command result parity and key read-model metrics.

---

## 22) Mandatory Reference Matrix (must-read by section and slice)

### 22.1 Rule of use
- Không được implement bất kỳ section/slice nào nếu chưa đọc đủ nhóm `Priority-1`.
- `Priority-2` là bắt buộc để validate design consistency.
- `Priority-3` đọc khi có thay đổi chạm boundary/query schema/performance.

### 22.2 Section -> Required references
| Plan Section | Priority-1 (read first) | Priority-2 (read before finalize) | Priority-3 (conditional) |
|---|---|---|---|
| `3) Discovery and Baseline` | `Favi-BE.API/Execution-Checklist.md` (Section 1), `Favi-BE.API/Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md` | `Favi-BE.API/Docs/CQRS-Modernization/Architecture-BoundedContexts.md`, `Favi-BE.API/Docs/CQRS-Modernization/Aggregate-Inventory.md` | `Favi-BE.API/Docs/CQRS-Modernization/Notification-Refactor-SignalR-MediatR.md` |
| `4) Target Architecture Definition` | `Favi-BE.API/Docs/CQRS-Modernization/Architecture-BoundedContexts.md`, `Favi-BE.API/Docs/CQRS-Modernization/BuildingBlocks-Design.md` | `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md`, `Favi-BE.API/Docs/CQRS-Modernization/ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` | `Favi-BE.API/Docs/CQRS-Modernization/Folder-Restructure-Mapping.md` |
| `5) Domain Decomposition Tasks` | `Favi-BE.API/Docs/CQRS-Modernization/Aggregate-Inventory.md`, `Favi-BE.API/Docs/CQRS-Modernization/Architecture-BoundedContexts.md` | `Favi-BE.API/Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md` | `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md` |
| `6) Command/Query Catalog` | `Favi-BE.API/Docs/CQRS-Modernization/CQRS-CommandQuery-Catalog.md`, `Favi-BE.API/Docs/CQRS-Modernization/Auth-CQRS-Catalog.md` | `Favi-BE.API/Docs/CQRS-Modernization/Aggregate-Inventory.md` | `Favi-BE.API/Docs/CQRS-Modernization/ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` |
| `7) Schema Evolution and Data Strategy` | `Favi-BE.API/Docs/CQRS-Modernization/Schema-Transition-Plan.md` | `Favi-BE.API/Docs/CQRS-Modernization/Outbox-Implementation-Guide.md`, `Favi-BE.API/Docs/CQRS-Modernization/Inbox-Implementation-Guide.md` | `Favi-BE.API/Docs/CQRS-Modernization/Notification-Refactor-SignalR-MediatR.md` |
| `8) Folder Restructure Plan` | `Favi-BE.API/Docs/CQRS-Modernization/Folder-Restructure-Mapping.md` | `Favi-BE.API/Docs/CQRS-Modernization/Architecture-BoundedContexts.md`, `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md` | `Favi-BE.API/Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md` |
| `9) Domain Events + Outbox` | `Favi-BE.API/Docs/CQRS-Modernization/Outbox-Implementation-Guide.md`, `Favi-BE.API/Docs/CQRS-Modernization/BuildingBlocks-Design.md` | `Favi-BE.API/Docs/CQRS-Modernization/Notification-Refactor-SignalR-MediatR.md`, `Favi-BE.API/Docs/CQRS-Modernization/Schema-Transition-Plan.md` | `Favi-BE.API/Docs/CQRS-Modernization/InternalCommands-Decision-Log.md` |
| `10) Inbox + InternalCommands` | `Favi-BE.API/Docs/CQRS-Modernization/Inbox-Implementation-Guide.md`, `Favi-BE.API/Docs/CQRS-Modernization/InternalCommands-Decision-Log.md` | `Favi-BE.API/Docs/CQRS-Modernization/Schema-Transition-Plan.md` | `Favi-BE.API/Docs/CQRS-Modernization/Outbox-Implementation-Guide.md` |
| `11) MediatR Adoption` | `Favi-BE.API/Docs/CQRS-Modernization/BuildingBlocks-Design.md`, `Favi-BE.API/Docs/CQRS-Modernization/CQRS-CommandQuery-Catalog.md` | `Favi-BE.API/Docs/CQRS-Modernization/Auth-CQRS-Catalog.md`, `Favi-BE.API/Docs/CQRS-Modernization/ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` | `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md` |
| `12) Notification Refactor` | `Favi-BE.API/Docs/CQRS-Modernization/Notification-Refactor-SignalR-MediatR.md` | `Favi-BE.API/Docs/CQRS-Modernization/Outbox-Implementation-Guide.md`, `Favi-BE.API/Docs/CQRS-Modernization/Inbox-Implementation-Guide.md` | `Favi-BE.API/Docs/CQRS-Modernization/CQRS-CommandQuery-Catalog.md` |
| `13) Module Boundary Enforcement` | `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md` | `Favi-BE.API/Docs/CQRS-Modernization/Architecture-BoundedContexts.md`, `Favi-BE.API/Docs/CQRS-Modernization/Favi-Concrete-Module-Aggregate-Matrix.md` | `Favi-BE.API/Docs/CQRS-Modernization/Folder-Restructure-Mapping.md` |
| `14) Strangler Migration Plan` | `Favi-BE.API/Docs/CQRS-Modernization/Strangler-Rollout-Plan.md`, `Favi-BE.API/Execution-Checklist.md` | `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md`, `Favi-BE.API/Docs/CQRS-Modernization/ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` | `Favi-BE.API/Docs/CQRS-Modernization/Schema-Transition-Plan.md` |
| `20) Read/Write Segregation Strategy` | `Favi-BE.API/Docs/CQRS-Modernization/ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` | `Favi-BE.API/Docs/CQRS-Modernization/CQRS-CommandQuery-Catalog.md`, `Favi-BE.API/Docs/CQRS-Modernization/Module-Boundary-Enforcement.md` | `Favi-BE.API/Docs/CQRS-Modernization/Schema-Transition-Plan.md` |

### 22.3 Slice -> Required references
| Slice | Priority-1 (must-read before code) | Priority-2 (must-read before merge) |
|---|---|---|
| `Foundation.CQRSOutbox` | `BuildingBlocks-Design.md`, `Outbox-Implementation-Guide.md`, `Inbox-Implementation-Guide.md`, `Schema-Transition-Plan.md` | `Module-Boundary-Enforcement.md`, `Strangler-Rollout-Plan.md` |
| `Auth.LoginCQRS` | `Auth-CQRS-Catalog.md`, `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md` | `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`, `Execution-Checklist.md` |
| `Notification.EventDriven` | `Notification-Refactor-SignalR-MediatR.md`, `Outbox-Implementation-Guide.md`, `Inbox-Implementation-Guide.md` | `Schema-Transition-Plan.md`, `Execution-Checklist.md` |
| `Engagement.Commands` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md`, `Favi-Concrete-Module-Aggregate-Matrix.md` | `Notification-Refactor-SignalR-MediatR.md`, `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md` |
| `SocialGraph.Commands` | `CQRS-CommandQuery-Catalog.md`, `Architecture-BoundedContexts.md`, `Favi-Concrete-Module-Aggregate-Matrix.md` | `Module-Boundary-Enforcement.md`, `Execution-Checklist.md` |
| `ContentPublishing.Commands` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md`, `Schema-Transition-Plan.md` | `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`, `Module-Boundary-Enforcement.md` |
| `Stories.CommandsAndExpiry` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md` | `Schema-Transition-Plan.md`, `Strangler-Rollout-Plan.md` |
| `Messaging.CQRS` | `CQRS-CommandQuery-Catalog.md`, `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`, `Module-Boundary-Enforcement.md` | `Schema-Transition-Plan.md`, `Execution-Checklist.md` |
| `Moderation.BackofficeCQRS` | `CQRS-CommandQuery-Catalog.md`, `Aggregate-Inventory.md`, `Module-Boundary-Enforcement.md` | `Strangler-Rollout-Plan.md`, `Execution-Checklist.md` |

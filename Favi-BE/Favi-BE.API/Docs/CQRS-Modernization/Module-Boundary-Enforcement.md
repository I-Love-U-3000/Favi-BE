# Module Boundary Enforcement

## 1. Boundary rules

1. API depends on module facades/contracts only — không reference module internal namespaces trực tiếp.
2. Module internal namespaces không được reference cross-module.
3. Cross-module communication bắt buộc qua integration events + outbox/inbox.

---

## 2. Implementation status

### 2.1 Facade contracts — 🗓 Planned (Slice 16 / R16)

Các facade contracts dưới đây là **target architecture**, chưa có file nào tồn tại trong codebase. Hiện tại API layer gọi thẳng `IMediator.Send(...)` với command/query types của từng module — đây là acceptable interim state trong giai đoạn strangler, nhưng chưa đạt boundary rule 1.

Sẽ implement trong **Slice 16 — Facade.ModuleContracts** (sau khi R9–R14 hoàn chỉnh). Xem chi tiết tại `Execution-Checklist.md` Section 19.

| Facade | Target location | Status |
|---|---|---|
| `IAuthFacade` | `Favi-BE.Modules.Auth/Application/IAuthFacade.cs` | 🗓 Planned — Slice 16 |
| `ISocialGraphFacade` | `Favi-BE.Modules.SocialGraph/Application/ISocialGraphFacade.cs` | 🗓 Planned — Slice 16 |
| `IContentPublishingFacade` | `Favi-BE.Modules.ContentPublishing/Application/IContentPublishingFacade.cs` | 🗓 Planned — Slice 16 |
| `IEngagementFacade` | `Favi-BE.Modules.Engagement/Application/IEngagementFacade.cs` | 🗓 Planned — Slice 16 |
| `INotificationsFacade` | `Favi-BE.Modules.Notifications/Application/INotificationsFacade.cs` | 🗓 Planned — Slice 16 |
| `IStoriesFacade` | `Favi-BE.Modules.Stories/Application/IStoriesFacade.cs` | 🗓 Planned — Slice 16 |
| `IMessagingFacade` | `Favi-BE.Modules.Messaging/Application/IMessagingFacade.cs` | 🗓 Planned — Slice 16 |
| `IModerationFacade` | `Favi-BE.Modules.Moderation/Application/IModerationFacade.cs` | 🗓 Planned — Slice 16 |

### 2.2 Cross-module communication — Partially implemented

| Boundary | Mechanism hiện tại | Status |
|---|---|---|
| Engagement → Notifications | `IEngagementNotificationService` adapter → `OutboxNotificationService` → Outbox → `IInboxConsumer` | ⚠️ Intermediate — adapter layer vẫn còn, target là domain events (Slice 13) |
| SocialGraph → Notifications | `ISocialGraphNotificationService` adapter → `OutboxNotificationService` → Outbox → `IInboxConsumer` | ⚠️ Intermediate — adapter layer vẫn còn, target là domain events (Slice 13) |
| Moderation → Identity | Qua contracts (không có direct module reference) | ✅ Correct |
| Messaging → Notifications | Không có direct cross-module call; realtime qua `IChatRealtimeGateway` adapter | ✅ Correct |
| Tất cả modules khác | Không có cross-module calls — isolated | ✅ Correct |

---

## 3. Dependency matrix

Ký hiệu: ✅ allowed and correctly wired | ⚠️ intermediate state — via adapter, target is domain events | ❌ direct cross-module reference — forbidden | — not applicable

| From \ To | Identity | SocialGraph | Content | Engagement | Notifications | Stories | Messaging | Moderation |
|---|---|---|---|---|---|---|---|---|
| Identity | ✅ | — | — | — | — | — | — | ✅ via contracts |
| SocialGraph | ✅ contract | ✅ | — | — | ⚠️ via adapter (→ Slice 13: domain events) | — | — | — |
| Content | ✅ contract | — | ✅ | ✅ via events | ✅ via events | — | — | — |
| Engagement | ✅ contract | — | ✅ contract | ✅ | ⚠️ via adapter (→ Slice 13: domain events) | — | — | ✅ via events |
| Notifications | ✅ contract | ✅ via events | ✅ via events | ✅ via events | ✅ | ✅ via events | ✅ via events | ✅ via events |
| Stories | ✅ contract | — | — | — | ✅ via events | ✅ | — | — |
| Messaging | ✅ contract | — | — | — | ✅ via events | — | ✅ | — |
| Moderation | ✅ contract | ✅ via events | ✅ via events | ✅ via events | ✅ via events | ✅ via events | ✅ via events | ✅ |

---

## 4. Architecture tests — trạng thái hiện tại

### 4.1 Đã có (NetArchTest, `Favi-BE.ArchitectureTests`)

| Test | File | Coverage |
|---|---|---|
| BuildingBlocks Domain không depend on Infrastructure/Application/API | `ArchitectureTests.cs` | ✅ |
| BuildingBlocks Application không depend on Infrastructure | `ArchitectureTests.cs` | ✅ |
| BuildingBlocks Infrastructure không depend on API | `ArchitectureTests.cs` | ✅ |
| Controllers không inject raw repositories | `ApiLayerArchitectureTests.cs` | ✅ |
| Engagement không depend on Auth/Notifications/SocialGraph application internals | `ApiLayerArchitectureTests.cs` | ✅ |
| Auth không depend on Engagement/Notifications application internals | `ApiLayerArchitectureTests.cs` | ✅ |
| SocialGraph không depend on Engagement/Auth/Notifications application internals | `ApiLayerArchitectureTests.cs` | ✅ |
| ContentPublishing không depend on Auth/Engagement/Notifications/SocialGraph application internals | `ApiLayerArchitectureTests.cs` | ✅ |
| Stories không depend on Auth/Engagement/Notifications/SocialGraph/ContentPublishing application internals | `StoriesModuleArchitectureTests.cs` | ✅ |
| Notifications consumers không depend on Engagement/Auth application internals | `NotificationsModuleArchitectureTests.cs` | ✅ |
| Messaging CommandHandlers không depend on Queries namespace | `MessagingModuleArchitectureTests.cs` | ✅ |

### 4.2 Còn thiếu — sẽ implement trong Slice 14–16

| Test cần thêm | Target file | Pending slice |
|---|---|---|
| Stories CommandHandlers không depend on Queries namespace | `StoriesModuleArchitectureTests.cs` | Slice 14 (sau Slice 9) |
| ContentPublishing CommandHandlers không depend on Queries namespace | `ContentPublishingModuleArchitectureTests.cs` | Slice 14 (sau Slice 10) |
| Notifications CommandHandlers không depend on Queries namespace | `NotificationsModuleArchitectureTests.cs` | Slice 14 (sau Slice 11) |
| Auth CommandHandlers không depend on Queries namespace | `AuthModuleArchitectureTests.cs` | Slice 14 (sau Slice 12) |
| Engagement CommandHandlers không depend on Queries namespace | `EngagementModuleArchitectureTests.cs` | Slice 14 |
| SocialGraph CommandHandlers không depend on Queries namespace | `SocialGraphModuleArchitectureTests.cs` | Slice 14 |
| Moderation CommandHandlers không depend on Queries namespace | `ModerationModuleArchitectureTests.cs` | Slice 14 |
| Controllers không inject `IStoryService` trực tiếp | `ApiLayerArchitectureTests.cs` | Slice 14 (sau Slice 9) |
| Controllers không inject `IPostService` trực tiếp | `ApiLayerArchitectureTests.cs` | Slice 14 (sau Slice 10) |
| Controllers không inject `INotificationService` trực tiếp | `ApiLayerArchitectureTests.cs` | Slice 14 (sau Slice 11) |
| Controllers không inject `IProfileService` trực tiếp | `ApiLayerArchitectureTests.cs` | Slice 14 (sau Slice 12) |
| Handlers không inject `ISocialGraphNotificationService` hoặc `IEngagementNotificationService` | `ApiLayerArchitectureTests.cs` | Slice 14 (sau Slice 13) |
| Controllers không inject `IMediator` trực tiếp (chỉ inject module facades) | `ApiLayerArchitectureTests.cs` | Slice 16 |
| API assembly không reference `*.Commands.*` hoặc `*.Queries.*` namespace của bất kỳ module nào | `ApiLayerArchitectureTests.cs` | Slice 16 |

---

## 5. Governance
- PR checklist requires boundary confirmation.
- Any exception must include ADR/decision log (xem `InternalCommands-Decision-Log.md` làm template).
- CI gate fail-fast khi architecture test fail.

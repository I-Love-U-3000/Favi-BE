# Folder Restructure Mapping (Standard DDD Modular Monolith)

## 1. Target structure (Sibling Projects)
- `Favi-BE.BuildingBlocks/` (Shared Kernel)
- `Favi-BE.Modules.Identity/` (Domain, App, Infra folders inside)
- `Favi-BE.Modules.SocialGraph/`
- `Favi-BE.Modules.ContentPublishing/`
- `Favi-BE.Modules.Engagement/`
- `Favi-BE.Modules.Notifications/`
- `Favi-BE.Modules.Stories/`
- `Favi-BE.Modules.Messaging/`
- `Favi-BE.Modules.Moderation/`
- `Favi-BE.API/` (The thin host)

## 2. Move map (current -> target)
| Source Path | Target Project | Target Folder | Rationale |
|---|---|---|---|
| `Favi-BE.API/Models/Entities/Profile.cs` | `Favi-BE.Modules.Identity` | `Domain/Aggregates/Profile/` | profile ownership |
| `Favi-BE.API/Models/Entities/EmailAccount.cs` | `Favi-BE.Modules.Identity` | `Domain/Aggregates/Auth/` | auth credential |
| `Favi-BE.API/Models/Entities/JoinTables/Follow.cs` | `Favi-BE.Modules.SocialGraph` | `Domain/Aggregates/Follow/` | graph boundary |
| `Favi-BE.API/Models/Entities/SocialLink.cs` | `Favi-BE.Modules.SocialGraph` | `Domain/Aggregates/Links/` | social links |
| `Favi-BE.API/Models/Entities/Post.cs` | `Favi-BE.Modules.ContentPublishing` | `Domain/Aggregates/Post/` | content core |
| `Favi-BE.API/Models/Entities/Collection.cs` | `Favi-BE.Modules.ContentPublishing` | `Domain/Aggregates/Collection/` | collection core |
| `Favi-BE.API/Models/Entities/Comment.cs` | `Favi-BE.Modules.Engagement` | `Domain/Aggregates/Comment/` | engagement split |
| `Favi-BE.API/Models/Entities/JoinTables/Reaction.cs` | `Favi-BE.Modules.Engagement` | `Domain/Aggregates/Reaction/` | reaction ownership |
| `Favi-BE.API/Models/Entities/Notification.cs` | `Favi-BE.Modules.Notifications` | `Domain/Aggregates/Notification/` | notification domain |
| `Favi-BE.API/Models/Entities/Story.cs` | `Favi-BE.Modules.Stories` | `Domain/Aggregates/Story/` | story ownership |
| `Favi-BE.API/Models/Entities/Conversation.cs` | `Favi-BE.Modules.Messaging` | `Domain/Aggregates/Conversation/` | messaging domain |
| `Favi-BE.API/Services/AuthService.cs` | `Favi-BE.Modules.Identity` | `Application/...` | service -> handlers |
| `Favi-BE.API/Services/NotificationService.cs` | `Favi-BE.Modules.Notifications` | `Application/...` | signalr refactor |

## 3. Assembly Dependency Rules (Standard)
- `BuildingBlocks` has ❌ NO dependencies on Modules or API.
- `Modules` depend only on `BuildingBlocks`.
- `Modules` ❌ DO NOT depend on each other (use Integration Events).
- `API` depends on `Modules` (via Facade/Command interfaces) and `BuildingBlocks`.

## 4. Migration strategy
- **Slice-by-slice**: Create one module project at a time.
- **Physical Boundary**: Use `internal` access modifiers for module-specific logic to enforce encapsulation.
# Ultimate AS-IS Dependency Map (Current Monolith)

Tài liệu này là bản **duy nhất** để nhìn dependency hiện tại của codebase theo góc nhìn hệ thống.
Mục tiêu: chỉ rõ thành phần nào đang gọi thành phần nào, điểm coupling cao, và chỗ cần tách.

---

## 1) System context diagram (AS-IS)

```plantuml
@startuml
skinparam componentStyle rectangle
left to right direction

actor Client
component "ASP.NET API (Monolith)" as API
component "SignalR Hub" as HUB
database "PostgreSQL\n(AppDbContext)" as PG
component "Redis\n(health + future cache)" as REDIS
component "VectorIndexService\n(HTTP client)" as VECTOR
component "NSFWService\n(HTTP client)" as NSFW
component "CloudinaryService" as CLOUD
component "Background Jobs\nPostCleanup + StoryExpiration" as JOBS
component "NotificationHub\nSignalR" as NHUB

Client --> API
Client --> HUB
API --> PG
API --> REDIS
API --> VECTOR
API --> NSFW
API --> CLOUD
API --> JOBS
API --> NHUB
VECTOR --> "Vector Index API"
"Vector Index API" --> "Qdrant"
@enduml
```

---

## 2) Internal module dependency diagram (AS-IS, logical grouping)

```plantuml
@startuml
skinparam componentStyle rectangle

together {
  component "Identity\n(AuthService, JwtService, ProfileService(part))" as ID
  component "SocialGraph\n(ProfileService follow/unfollow)" as SG
  component "Content\n(PostService, StoryService, CollectionService, TagService)" as CT
  component "Engagement\n(CommentService + PostService(reaction/share))" as EG
  component "Discovery\n(PostService(feed query), SearchService, VectorIndexService)" as DC
  component "Notification\n(NotificationService, NotificationsController)" as NT
  component "Admin/Moderation\n(Analytics/Audit/Bulk/Reports/UserModeration)" as AD
  component "Realtime/Chat\n(ChatService, ChatRealtimeService, ChatHub)" as CH
  component "Platform\n(UnitOfWork, PrivacyGuard, HostedServices, HealthChecks)" as PL
}

database "Shared PostgreSQL\n(AppDbContext)" as PG
component "Redis" as REDIS
component "Cloudinary" as CLOUD
component "Vector API/Qdrant" as VEC

ID --> PG
SG --> PG
CT --> PG
EG --> PG
DC --> PG
NT --> PG
AD --> PG
CH --> PG

DC --> REDIS
CT --> CLOUD
DC --> VEC
CT --> VEC
CT --> NT
EG --> NT
SG --> NT

ID --> PL
SG --> PL
CT --> PL
EG --> PL
DC --> PL
NT --> PL
AD --> PL
CH --> PL
@enduml
```

---

## 3) Service-level call map (bám sát code hiện tại)

## 3.1 Auth + Identity
- `AuthService` -> `IEmailAccountRepository`, `IProfileRepository`, `IJwtService`, `IUnitOfWork`.
- `ProfileService` -> `IUnitOfWork` (`Profiles`, `Follows`, `EmailAccounts`, `SocialLinks`), `ICloudinaryService`, `INotificationService`.

## 3.2 Content + Engagement
- `PostService` -> `IUnitOfWork`, `ICloudinaryService`, `IPrivacyGuard`, `IVectorIndexService`, `INSFWService`, optional `INotificationService`, optional `IAuditService`.
- `CommentService` -> `IUnitOfWork`, optional `INotificationService`, optional `IAuditService`.

## 3.3 Discovery/Search
- `SearchService` -> `IUnitOfWork`, `IVectorIndexService`, `IPrivacyGuard`.
- Feed query hiện nằm trong `PostService` (chưa tách query service riêng).

## 3.4 Notification + Realtime
- `NotificationService` -> `IUnitOfWork` + `IHubContext<NotificationHub>` (SignalR push realtime).

## 3.5 Platform/Cross-cutting
- `UnitOfWork` expose gần như toàn bộ repository -> coupling cao.
- `Program.cs` đăng ký hosted services:
  - `PostCleanupService`
  - `StoryExpirationService`
- Health checks: DB, Memory, Redis, Qdrant, Vector API.

---

## 4) Coupling hotspots (nơi cần tách trước)

1. `PostService` vừa command vừa query (write + feed read + related/search fallback).
2. `ProfileService` gộp identity + social graph + notification side-effect.
3. `NotificationService` vừa persist vừa realtime push.
4. `UnitOfWork` shared cho mọi domain -> module boundary mờ.
5. Async side-effects (vector/nsfw/noti) chưa đi qua outbox/bus chuẩn hóa.

---

## 5) Đề xuất tách boundary từ AS-IS

Bước 1 (trong modular monolith):
- Tách `PostService` thành:
  - `PostCommandService`
  - `FeedQueryService`
- Tách `ProfileService` thành:
  - `IdentityProfileService`
  - `SocialGraphService`
- Tách `NotificationService` thành:
  - `NotificationQueryService` (API read/mark)
  - `NotificationFanoutHandler` (async side-effects)

Bước 2:
- Chuẩn hóa event contracts + outbox.
- Chỉ cho phép dependency chéo module qua contracts/events.

Bước 3:
- Extract process theo thứ tự:
  1) Worker
  2) Read API
  3) Write API

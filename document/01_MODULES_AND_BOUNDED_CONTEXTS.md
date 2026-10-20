# 01 — Bounded Contexts, Danh Mục Module & Trách Nhiệm (Favi-BE)

## 1. Bản Đồ Bounded Contexts (Context Map)

Hệ thống Favi-BE bao gồm **9 Bounded Contexts** cùng với Shared Kernel (`Favi-BE.BuildingBlocks`):

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                             Favi-BE.BuildingBlocks                               │
│                   (Domain, Application, Infrastructure Kernel)                   │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
 ┌───────────────────────┬───────────────┴───────────────┬───────────────────────┐
 │                       │                               │                       │
 ▼                       ▼                               ▼                       ▼
Favi-BE.                Favi-BE.                        Favi-BE.                Favi-BE.
Modules.Auth            Modules.SocialGraph             Modules.ContentPub.     Modules.ContentDisc.
(Identity & Access)     (Follow & Relationships)        (Write: Posts/Media)    (Read: Feeds/Search)
 │                       │                               │                       │
 └───────────────────────┼───────────────────────────────┼───────────────────────┘
                         │                               │
 ┌───────────────────────┼───────────────────────────────┼───────────────────────┐
 │                       │                               │                       │
 ▼                       ▼                               ▼                       ▼
Favi-BE.                Favi-BE.                        Favi-BE.                Favi-BE.
Modules.Engagement      Modules.Notifications           Modules.Stories         Modules.Messaging
(Comment & Reaction)    (Unread & Realtime)             (24h Ephemeral Content) (Chat & Conversations)
                         │
                         ▼
                Favi-BE.Modules.Moderation
                (Trust, Reports, Bans & Admin)
```

---

## 2. Chi Tiết Trách Nhiệm Từng Module

### 2.1 Shared Kernel: `Favi-BE.BuildingBlocks`
Cung cấp các nền tảng dùng chung nhưng **không chứa business logic cụ thể của nghiệp vụ**:
- **Domain Kernel**:
  - `Entity<TId>`: Base class cho Aggregate Roots, quản lý danh sách `IDomainEvent` phát sinh trong chu kỳ sống.
  - `ValueObject`: Base class so sánh bằng giá trị (equality by value), bất biến (immutable).
  - `IBusinessRule`: Interface chuẩn hóa luật bất biến (invariant), ném `BusinessRuleValidationException` khi vi phạm.
  - `TypedIdValueBase`: Strongly-typed ID (chống nhầm lẫn giữa `UserId`, `PostId`, `CommentId`).
- **Application Kernel**:
  - `IOutbox`: Contract ghi nhận Integration Event vào outbox storage trong cùng transaction.
  - `IInbox`: Contract kiểm tra và đánh dấu idempotency cho consumer `(messageId, consumerName)`.
  - `IExecutionContextAccessor`: Truy xuất User ID, Request ID, Tenant ID và correlation context.
  - `IDomainEventNotification<T>`: Adapter chuyển đổi Domain Event sang MediatR Notification.
- **Infrastructure Kernel**:
  - `OutboxProcessor`: `IHostedService` định kỳ quét outbox, deserialize event và dispatch qua consumer.
  - `InboxProcessor`: Quản lý lifecycle xử lý của message và giải phóng lock khi lỗi.
  - `DomainEventsDispatcher`: Quét ChangeTracker của EF Core để thu thập và publish domain events trước khi commit.

---

### 2.2 Module `Identity & Access` (`Favi-BE.Modules.Auth`)
- **Trách nhiệm**: Đăng ký, đăng nhập (JWT + Refresh Token), xác thực hai lớp, quản lý phiên làm việc (`AuthSession`), cập nhật hồ sơ cá nhân và quyền riêng tư (`Profile`).
- **Aggregate Roots**: `Profile`, `EmailAccount`, `AuthSession`.
- **Dữ liệu sở hữu**: Các bảng `profiles`, `email_accounts`, `auth_sessions`.
- **Ports (Interfaces)**:
  - `IAuthWriteRepository`: Lưu trữ và cập nhật trạng thái account/profile.
  - `IJwtTokenService`: Sinh và xác thực JWT token, claims.
  - `IAuthQueryReader`: Đọc thông tin profile và credential với `.AsNoTracking()`.
- **Published Events**: `UserRegisteredEvent`, `UserLoggedInEvent`, `UserLoggedOutEvent`, `ProfileUpdatedEvent`.
- **Consumed Events**: `UserModeratedEvent`, `UserUnbannedEvent`.

---

### 2.3 Module `Social Graph` (`Favi-BE.Modules.SocialGraph`)
- **Trách nhiệm**: Quản lý quan hệ mạng xã hội hai chiều và một chiều (Follow / Unfollow / Block), danh sách mạng xã hội liên kết (`SocialLinks`), danh sách người theo dõi / đang theo dõi.
- **Aggregate Roots**: `FollowRelationship`, `SocialLink`.
- **Dữ liệu sở hữu**: Bảng `follows`, `social_links`.
- **Published Events**: `UserFollowedEvent`, `UserUnfollowedEvent`, `SocialLinkAddedEvent`, `SocialLinkRemovedEvent`.
- **Consumed Events**: `UserDeletedEvent`, `UserModeratedEvent`.

---

### 2.4 Module `Content Publishing` (`Favi-BE.Modules.ContentPublishing`)
- **Trách nhiệm (Write Only)**: Tạo bài viết mới, chỉnh sửa caption, upload media, gắn hashtags/tags, tạo và quản lý bộ sưu tập (`Collections`), chia sẻ lại bài viết (`Reposts`).
- **Aggregate Roots**: `Post`, `Collection`, `Repost`, `TagCatalog`.
- **Dữ liệu sở hữu**: Bảng `posts`, `post_media`, `post_tags`, `collections`, `post_collections`, `reposts`, `tags`.
- **Published Events**: `PostCreatedEvent`, `PostUpdatedEvent`, `PostDeletedEvent`, `CollectionUpdatedEvent`, `RepostSharedEvent`.
- **Consumed Events**: `UserModeratedEvent` (để ẩn hoặc hạ bài viết của tài khoản bị phạt).

---

### 2.5 Module `Content Discovery` (`Favi-BE.Modules.ContentDiscovery`)
- **Trách nhiệm (Read Only)**: Tổng hợp bảng tin (Personalized Feed & Guest Feed), phân trang bài viết (Cursor-based & Offset pagination), chi tiết bài viết, chi tiết bộ sưu tập, tìm kiếm theo từ khóa và tích hợp kết quả Vector Search.
- **Aggregate Roots**: **None** (Pure Projection Context — không có write path, không lưu aggregate state).
- **Dữ liệu sở hữu**: Không own bảng dữ liệu trực tiếp — đọc dữ liệu từ `posts`, `post_media`, `post_tags`, `collections`, `reactions`, `comments`, `follows` qua các read-only readers.
- **Published Events**: Không publish events.
- **Consumed Events**: `PostCreatedEvent`, `PostUpdatedEvent`, `PostDeletedEvent` (phục vụ cache invalidation).

---

### 2.6 Module `Engagement` (`Favi-BE.Modules.Engagement`)
- **Trách nhiệm**: Hệ thống bình luận đa cấp (Nested comment tree), tương tác cảm xúc (Reactions: Like, Heart, Laugh...) cho Post, Comment, Story, Repost. Đảm bảo Idempotency khi người dùng click liên tục (Toggle Reaction).
- **Aggregate Roots**: `CommentThread`, `Reaction`.
- **Dữ liệu sở hữu**: Bảng `comments`, `reactions`.
- **Published Events**: `CommentCreatedEvent`, `CommentDeletedEvent`, `ReactionToggledEvent`.
- **Consumed Events**: `PostDeletedEvent`, `CollectionDeletedEvent`, `RepostDeletedEvent`.

---

### 2.7 Module `Notifications` (`Favi-BE.Modules.Notifications`)
- **Trách nhiệm**: Lưu trữ lịch sử thông báo, tính toán số lượng chưa đọc (unread counter), đẩy thông báo thời gian thực ra WebSocket qua SignalR `NotificationHub`.
- **Aggregate Roots**: `Notification`.
- **Dữ liệu sở hữu**: Bảng `notifications`.
- **Published Events**: `NotificationCreatedEvent`, `NotificationReadEvent`, `UnreadCountChangedEvent`.
- **Consumed Events**: `UserFollowedEvent`, `CommentCreatedEvent`, `ReactionToggledEvent`, `PostCreatedEvent`.

---

### 2.8 Module `Stories` (`Favi-BE.Modules.Stories`)
- **Trách nhiệm**: Nội dung tạm thời tự biến mất sau 24 giờ. Theo dõi người xem story (`StoryViews`), phát hiện câu chuyện đã hết hạn (`StoryExpired`).
- **Aggregate Roots**: `Story`.
- **Dữ liệu sở hữu**: Bảng `stories`, `story_views`.
- **Published Events**: `StoryCreatedEvent`, `StoryArchivedEvent`, `StoryExpiredEvent`, `StoryViewedEvent`.
- **Consumed Events**: `UserModeratedEvent`.

---

### 2.9 Module `Messaging` (`Favi-BE.Modules.Messaging`)
- **Trách nhiệm**: Cuộc trò chuyện trực tiếp (1-1 Direct Message) và trò chuyện nhóm (Group Chat), gửi tin nhắn văn bản, hình ảnh, trạng thái đã đọc (`MessageRead`), tích hợp SignalR `ChatHub` và WebRTC Signaling Hub (`CallHub`).
- **Aggregate Roots**: `Conversation`, `Message`.
- **Dữ liệu sở hữu**: Bảng `conversations`, `messages`, `user_conversations`, `message_reads`.
- **Published Events**: `MessageSentEvent`, `ConversationReadEvent`, `ConversationCreatedEvent`.
- **Consumed Events**: `UserDeletedEvent`, `UserModeratedEvent`.

---

### 2.10 Module `Moderation & Trust` (`Favi-BE.Modules.Moderation`)
- **Trách nhiệm**: Tiếp nhận báo cáo vi phạm (`Reports`), quy trình xử lý của Admin/Moderator, khóa tài khoản (Ban/Mute), ghi nhận nhật ký kiểm toán quản trị (`AdminAuditLogs`), thống kê Dashboard Analytics.
- **Aggregate Roots**: `ReportCase`, `UserModeration`, `AdminAction`.
- **Dữ liệu sở hữu**: Bảng `reports`, `user_moderations`, `admin_actions`.
- **Published Events**: `ReportResolvedEvent`, `UserModeratedEvent`, `AdminActionLoggedEvent`.
- **Consumed Events**: Các domain event liên quan đến vi phạm từ Content, Comments, Profiles.

---

## 3. Ma Trận Quan Hệ Phụ Thuộc (Dependency Matrix)

Quy tắc bất biến:
- `BuildingBlocks` **không phụ thuộc** vào bất kỳ Module nào.
- Các `Modules` **không phụ thuộc trực tiếp** vào nhau (Không project reference lẫn nhau).
- Giao tiếp giữa các module chỉ thông qua **Event Bus (Outbox/Inbox)** hoặc **Application Contracts/Facades** nằm tại API Host.

| Source Module \ Target | Auth | SocialGraph | ContentPub | ContentDisc | Engagement | Notifications | Stories | Messaging | Moderation |
|---|---|---|---|---|---|---|---|---|---|
| **Auth** | — | — | — | — | — | — | — | — | Contract |
| **SocialGraph** | Contract | — | — | — | — | Event (Outbox) | — | — | — |
| **ContentPub** | Contract | — | — | — | Event (Outbox) | Event (Outbox) | — | — | — |
| **ContentDisc** | Read Port | Read Port | Read Port | — | Read Port | — | — | — | — |
| **Engagement** | Contract | — | Contract | — | — | Event (Outbox) | — | — | Event |
| **Notifications**| Contract | Event | Event | — | Event | — | Event | Event | Event |
| **Stories** | Contract | — | — | — | — | Event (Outbox) | — | — | — |
| **Messaging** | Contract | — | — | — | — | Event (Outbox) | — | — | — |
| **Moderation** | Contract | Event | Event | — | Event | Event | Event | Event | — |

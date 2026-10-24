# 01 — Bounded Contexts, Module Catalog & Responsibilities (Favi-BE)

## 1. Bounded Contexts Map (Context Map)

The Favi-BE system comprises **9 Bounded Contexts** along with a Shared Kernel (`Favi-BE.BuildingBlocks`):

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                             Favi-BE.BuildingBlocks                               │
│                   (Domain, Application, Infrastructure Kernel)                   │
│                                                                                  │
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

## 2. Module Responsibilities & Specifications

### 2.1 Shared Kernel: `Favi-BE.BuildingBlocks`
Provides shared foundational building blocks without encapsulating **business-specific domain logic**:
- **Domain Kernel**:
  - `Entity<TId>`: Base class for Aggregate Roots, managing lifecycle domain events (`IDomainEvent`).
  - `ValueObject`: Base class supporting equality by value and immutability.
  - `IBusinessRule`: Standard interface for domain invariants; throws `BusinessRuleValidationException` when violated.
  - `TypedIdValueBase`: Strongly-typed IDs to prevent ID confusion (e.g., mixing `UserId`, `PostId`, and `CommentId`).
- **Application Kernel**:
  - `IOutbox`: Contract for persisting Integration Events into outbox storage in the same database transaction.
  - `IInbox`: Contract for idempotency checking and consumer tracking: `(messageId, consumerName)`.
  - `IExecutionContextAccessor`: Provides execution context including User ID, Request ID, Tenant ID, and correlation tokens.
  - `IDomainEventNotification<T>`: Adapter converting Domain Events into MediatR Notifications.
- **Infrastructure Kernel**:
  - `OutboxProcessor`: Background `IHostedService` periodically polling the outbox, deserializing events, and dispatching them to consumers.
  - `InboxProcessor`: Manages message processing lifecycles and safely releases locks on failures.
  - `DomainEventsDispatcher`: Scans EF Core ChangeTracker to gather and publish domain events prior to commit.

---

### 2.2 Module `Identity & Access` (`Favi-BE.Modules.Auth`)
- **Responsibilities**: Registration, login (JWT + Refresh Tokens), two-factor authentication, session management (`AuthSession`), profile management, and privacy preferences (`Profile`).
- **Aggregate Roots**: `Profile`, `EmailAccount`, `AuthSession`.
- **Owned Tables**: `profiles`, `email_accounts`, `auth_sessions`.
- **Ports (Interfaces)**:
  - `IAuthWriteRepository`: Persists and updates account/profile aggregates.
  - `IJwtTokenService`: Generates and validates JWT tokens and claims.
  - `IAuthQueryReader`: Reads profile information and credentials using `.AsNoTracking()`.
- **Published Events**: `UserRegisteredEvent`, `UserLoggedInEvent`, `UserLoggedOutEvent`, `ProfileUpdatedEvent`.
- **Consumed Events**: `UserModeratedEvent`, `UserUnbannedEvent`.

---

### 2.3 Module `Social Graph` (`Favi-BE.Modules.SocialGraph`)
- **Responsibilities**: Manages unidirectional and bidirectional social relationships (Follow / Unfollow / Block), external social links (`SocialLinks`), and followers/following counts and rosters.
- **Aggregate Roots**: `FollowRelationship`, `SocialLink`.
- **Owned Tables**: `follows`, `social_links`.
- **Published Events**: `UserFollowedEvent`, `UserUnfollowedEvent`, `SocialLinkAddedEvent`, `SocialLinkRemovedEvent`.
- **Consumed Events**: `UserDeletedEvent`, `UserModeratedEvent`.

---

### 2.4 Module `Content Publishing` (`Favi-BE.Modules.ContentPublishing`)
- **Responsibilities (Write Only)**: Post creation, caption updates, media upload coordination, hashtag cataloging, collection management (`Collections`), and resharing/reposting (`Reposts`).
- **Aggregate Roots**: `Post`, `Collection`, `Repost`, `TagCatalog`.
- **Owned Tables**: `posts`, `post_media`, `post_tags`, `collections`, `post_collections`, `reposts`, `tags`.
- **Published Events**: `PostCreatedEvent`, `PostUpdatedEvent`, `PostDeletedEvent`, `CollectionUpdatedEvent`, `RepostSharedEvent`.
- **Consumed Events**: `UserModeratedEvent` (to hide or demote posts belonging to restricted accounts).

---

### 2.5 Module `Content Discovery` (`Favi-BE.Modules.ContentDiscovery`)
- **Responsibilities (Read Only)**: Aggregating feeds (Personalized Feed & Guest Feed), article and post pagination (Cursor-based & Offset pagination), post details, collection overviews, keyword search, and Vector Search result integration.
- **Aggregate Roots**: **None** (Pure Projection Context — contains no write paths or persistent aggregate states).
- **Owned Data**: Does not directly own primary write tables; projects read models from `posts`, `post_media`, `post_tags`, `collections`, `reactions`, `comments`, and `follows` via read-only readers.
- **Published Events**: None.
- **Consumed Events**: `PostCreatedEvent`, `PostUpdatedEvent`, `PostDeletedEvent` (used for cache invalidation).

---

### 2.6 Module `Engagement` (`Favi-BE.Modules.Engagement`)
- **Responsibilities**: Hierarchical comment threads (nested comment trees) and emotional reactions (Like, Heart, Laugh...) on Posts, Comments, Stories, and Reposts. Guarantees idempotency on rapid toggling (Reaction Toggles).
- **Aggregate Roots**: `CommentThread`, `Reaction`.
- **Owned Tables**: `comments`, `reactions`.
- **Published Events**: `CommentCreatedEvent`, `CommentDeletedEvent`, `ReactionToggledEvent`.
- **Consumed Events**: `PostDeletedEvent`, `CollectionDeletedEvent`, `RepostDeletedEvent`.

---

### 2.7 Module `Notifications` (`Favi-BE.Modules.Notifications`)
- **Responsibilities**: Persisting notification history, aggregating unread counters, and pushing real-time notifications over WebSockets via SignalR `NotificationHub`.
- **Aggregate Roots**: `Notification`.
- **Owned Tables**: `notifications`.
- **Published Events**: `NotificationCreatedEvent`, `NotificationReadEvent`, `UnreadCountChangedEvent`.
- **Consumed Events**: `UserFollowedEvent`, `CommentCreatedEvent`, `ReactionToggledEvent`, `PostCreatedEvent`.

---

### 2.8 Module `Stories` (`Favi-BE.Modules.Stories`)
- **Responsibilities**: Ephemeral content that automatically expires after 24 hours. Tracking story viewers (`StoryViews`) and pruning expired stories (`StoryExpired`).
- **Aggregate Roots**: `Story`.
- **Owned Tables**: `stories`, `story_views`.
- **Published Events**: `StoryCreatedEvent`, `StoryArchivedEvent`, `StoryExpiredEvent`, `StoryViewedEvent`.
- **Consumed Events**: `UserModeratedEvent`.

---

### 2.9 Module `Messaging` (`Favi-BE.Modules.Messaging`)
- **Responsibilities**: 1-on-1 direct messaging and group chats, text and multimedia messaging, read receipts (`MessageRead`), integration with SignalR `ChatHub` and WebRTC signaling (`CallHub`).
- **Aggregate Roots**: `Conversation`, `Message`.
- **Owned Tables**: `conversations`, `messages`, `user_conversations`, `message_reads`.
- **Published Events**: `MessageSentEvent`, `ConversationReadEvent`, `ConversationCreatedEvent`.
- **Consumed Events**: `UserDeletedEvent`, `UserModeratedEvent`.

---

### 2.10 Module `Moderation & Trust` (`Favi-BE.Modules.Moderation`)
- **Responsibilities**: Intake of abuse reports (`Reports`), admin/moderator review workflows, account suspensions (Ban/Mute), audit logging (`AdminAuditLogs`), and moderation analytics.
- **Aggregate Roots**: `ReportCase`, `UserModeration`, `AdminAction`.
- **Owned Tables**: `reports`, `user_moderations`, `admin_actions`.
- **Published Events**: `ReportResolvedEvent`, `UserModeratedEvent`, `AdminActionLoggedEvent`.
- **Consumed Events**: Content-, Comment-, and Profile-related domain violation events.

---

## 3. Dependency Matrix

Architectural Invariants:
- `BuildingBlocks` **never depends** on any module.
- `Modules` **never depend directly** on one another (no direct project references).
- Inter-module interactions occur strictly via the **Event Bus (Outbox/Inbox)** or **Application Contracts/Facades** registered at the API Host.

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

# Architecture Bounded Contexts (Favi-BE)

## 1. Mục tiêu
Tài liệu này chốt ranh giới domain cho lộ trình CQRS + Outbox + MediatR + Strangler.

## 2. Context map
| Bounded Context | Core Responsibility | Aggregate Roots | Owns Data | Publishes Events | Consumes Events |
|---|---|---|---|---|---|
| Identity & Access | Đăng nhập, token, profile cơ bản, session | `Profile`, `EmailAccount`, `AuthSession` (new) | `profiles`, `email_accounts`, `auth_sessions` (new) | `UserRegistered`, `UserLoggedIn`, `UserLoggedOut`, `ProfileUpdated` | `UserModerated`, `UserUnbanned` |
| Social Graph | Quan hệ follow và social links | `FollowRelationship`, `SocialLink` | `follows`, `social_links` | `UserFollowed`, `UserUnfollowed`, `SocialLinkAdded`, `SocialLinkRemoved` | `UserDeleted` |
| Content Publishing | Post, media, tag, collection, repost — **write only** (commands) | `Post`, `Collection`, `Repost`, `TagCatalog` | `posts`, `post_media`, `post_tags`, `collections`, `post_collections`, `reposts`, `tags` | `PostCreated`, `PostUpdated`, `PostDeleted`, `CollectionUpdated`, `RepostShared` | `UserModerated` |
| Content Discovery | Feed aggregation, post/collection/repost read projections — **read only** (queries). Không có aggregate, không có write path. Aggregates data từ ContentPublishing + Engagement + SocialGraph qua read-only projections. | None (pure projection) | Không own table — reads từ `posts`, `post_media`, `post_tags`, `collections`, `post_collections`, `reposts`, `tags`, `reactions`, `comments`, `follows` | None | `PostCreated`, `PostUpdated`, `PostDeleted` (future: cache invalidation) |
| Engagement | Comment + reaction cho post/comment/collection/repost | `CommentThread`, `Reaction` | `comments`, `reactions` | `CommentCreated`, `CommentUpdated`, `CommentDeleted`, `ReactionToggled` | `PostDeleted`, `CollectionDeleted`, `RepostDeleted` |
| Notifications | Notification persistence + unread projection + realtime dispatch adapter | `Notification` | `notifications` | `NotificationCreated`, `NotificationRead`, `UnreadCountChanged` | `UserFollowed`, `CommentCreated`, `ReactionToggled`, `PostCreated` |
| Stories | Story lifecycle + views + expiry | `Story` | `stories`, `story_views` | `StoryCreated`, `StoryArchived`, `StoryExpired`, `StoryViewed` | `UserModerated` |
| Messaging | Conversation + message + read state | `Conversation`, `Message` | `conversations`, `messages`, `user_conversations`, `message_reads` | `MessageSent`, `ConversationRead`, `ConversationCreated` | `UserDeleted`, `UserModerated` |
| Moderation & Trust | Report, moderation actions, admin audit | `ReportCase`, `UserModeration`, `AdminAction` | `reports`, `user_moderations`, `admin_actions` | `ReportCreated`, `ReportResolved`, `UserModerated`, `AdminActionLogged` | all domain events (for policy enforcement) |

## 3. Ownership rules (hard boundaries)
1. Mỗi aggregate chỉ có một owner context.
2. Cross-context interaction bắt buộc qua integration events/outbox-inbox.
3. API layer không gọi chéo service nội bộ context khác.
4. Context chỉ expose facade contracts ở application layer.

## 4. Decision đã chốt
- `GetFollowersQuery` và `GetFollowingsQuery` thuộc `Social Graph` (`FollowRelationship`), không thuộc `Identity & Access`.
- `Identity & Access` chỉ giữ profile/auth/session concerns.
- `Content Publishing` chỉ giữ write path (commands). Toàn bộ query/read path (feed, post detail, collection listing, repost listing) thuộc `Content Discovery`.
- `Content Discovery` là pure read context: không có aggregate, không có write, không publish events. Adapter trong API layer được phép query từ nhiều nguồn (ContentPublishing tables, Engagement query reader, SocialGraph query reader) để assemble read projections.

## 5. Upstream/Downstream dependency
| Context | Upstream dependencies | Downstream consumers |
|---|---|---|
| Identity & Access | None | Social Graph, Messaging, Moderation |
| Social Graph | Identity & Access | Notifications, Feed/Content ranking |
| Content Publishing | Identity & Access | Content Discovery, Engagement, Notifications, Search |
| Content Discovery | Content Publishing, Engagement, Social Graph, Identity & Access | API clients (feed, post detail, collection listing) |
| Engagement | Content Publishing, Identity & Access | Notifications |
| Notifications | Social Graph, Engagement, Content Publishing | SignalR clients |
| Stories | Identity & Access | Notifications |
| Messaging | Identity & Access | Notifications |
| Moderation & Trust | Identity & Access, Content, Engagement, Stories, Messaging | Identity & Access, Content, Engagement |

## 6. Anti-corruption & contracts
- Event contracts versioned: `v1` suffix cho payload contract namespace.
- Không chia sẻ EF entities giữa contexts.
- Shared primitives đặt ở `BuildingBlocks` (IDs, domain events, rule exceptions, execution context).
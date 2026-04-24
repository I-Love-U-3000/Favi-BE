# Aggregate Inventory (Favi-BE)

## 1. Tổng quan
- Tổng bounded contexts: `8`
- Tổng aggregate roots mục tiêu: `16`

## 2. Inventory chi tiết
| Context | Aggregate Root | Owned Entities/Value Objects | Invariants chính | Transaction boundary |
|---|---|---|---|---|
| Identity & Access | `Profile` | profile settings, privacy settings | Username duy nhất; trạng thái ban hợp lệ | Profile update/write |
| Identity & Access | `EmailAccount` | credentials metadata | Email duy nhất; password hash bắt buộc | Register/change credential |
| Identity & Access | `AuthSession` (new) | refresh token/session metadata | 1 token phải revoke/expire rõ ràng | Login/refresh/logout |
| Social Graph | `FollowRelationship` | follower-followee edge | Không self-follow; không duplicate edge | Follow/unfollow |
| Social Graph | `SocialLink` | provider + url | URL hợp lệ; không duplicate provider per profile (policy) | Add/remove social link |
| Content Publishing | `Post` | `PostMedia`, `PostTag` | Owner-only mutate; ordered media positions; privacy hợp lệ | Create/update/delete/archive post |
| Content Publishing | `Collection` | `PostCollection` membership | Owner-only mutate; membership unique | Collection CRUD + add/remove post |
| Content Publishing | `Repost` | repost metadata | Một actor không repost duplicate cùng target (policy) | Share/unshare repost |
| Content Publishing | `TagCatalog` | canonical tag entries | Tag normalized unique | Create/merge tag |
| Engagement | `CommentThread` | replies tree | Parent-child hợp lệ; owner/moderation delete rules | Create/update/delete comment |
| Engagement | `Reaction` | reaction target ref | Mỗi user 1 reaction per target | Toggle reaction |
| Notifications | `Notification` | unread counter projection data | Không notify self cho events cần chặn; read state idempotent | Create/read/delete notification |
| Stories | `Story` | `StoryView` | TTL 24h; owner-only archive/delete; no duplicate views per viewer | Create/archive/delete/expire story |
| Messaging | `Conversation` | `UserConversation` members | Member authorization; DM uniqueness policy | Create DM/group, membership change |
| Messaging | `Message` | `MessageRead` | Sender member of conversation; read marker monotonic | Send/read message |
| Moderation & Trust | `ReportCase` | report details | Reporter/target hợp lệ; lifecycle Open->Resolved | Create/resolve report |
| Moderation & Trust | `UserModeration` | sanction windows | Ban window consistency; revoke allowed state | Moderate/revoke |
| Moderation & Trust | `AdminAction` | audit metadata | immutable audit trail | Log admin actions |

## 3. Aggregate counts by context
| Context | Aggregate count |
|---|---|
| Identity & Access | 3 |
| Social Graph | 2 |
| Content Publishing | 4 |
| Engagement | 2 |
| Notifications | 1 |
| Stories | 1 |
| Messaging | 2 |
| Moderation & Trust | 3 |

## 4. Notes for migration
- Các aggregate mới (`AuthSession`, `FollowRelationship` module ownership rõ) triển khai additive trước.
- Invariants bắt buộc đi vào domain model (`IBusinessRule`) trước khi mở rộng handlers.
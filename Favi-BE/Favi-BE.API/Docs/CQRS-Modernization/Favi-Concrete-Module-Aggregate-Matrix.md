# Favi Concrete Module-Aggregate Matrix

## 1. Entity -> Module -> Aggregate ownership
| Current Entity | Target Module | Target Aggregate Root | Notes |
|---|---|---|---|
| `Profile` | Identity & Access | `Profile` | profile core + privacy setting |
| `EmailAccount` | Identity & Access | `EmailAccount` | local auth credential |
| `Follow` | Social Graph | `FollowRelationship` | relationship edge ownership |
| `SocialLink` | Social Graph | `SocialLink` | profile external links |
| `Post` | Content Publishing | `Post` | post core aggregate |
| `PostMedia` | Content Publishing | `Post` | child entity |
| `PostTag` | Content Publishing | `Post` | child relation |
| `Tag` | Content Publishing | `TagCatalog` | canonical tag list |
| `Collection` | Content Publishing | `Collection` | owner collection |
| `PostCollection` | Content Publishing | `Collection` | collection membership |
| `Repost` | Content Publishing | `Repost` | repost aggregate |
| `Comment` | Engagement | `CommentThread` | replies thread |
| `Reaction` | Engagement | `Reaction` | toggle reaction |
| `Notification` | Notifications | `Notification` | user notification + read state |
| `Story` | Stories | `Story` | story lifecycle |
| `StoryView` | Stories | `Story` | owned view record |
| `Conversation` | Messaging | `Conversation` | chat context |
| `Message` | Messaging | `Message` | message aggregate |
| `UserConversation` | Messaging | `Conversation` | participant relation |
| `MessageRead` | Messaging | `Message` | read marker |
| `Report` | Moderation & Trust | `ReportCase` | moderation report |
| `UserModeration` | Moderation & Trust | `UserModeration` | sanction actions |
| `AdminAction` | Moderation & Trust | `AdminAction` | audit log |

## 2. Service -> Module mapping
| Current Service | Target Module |
|---|---|
| `AuthService` | Identity & Access |
| `ProfileService` | Identity & Access + Social Graph split |
| `PostService` | Content Publishing + Engagement split |
| `CollectionService` | Content Publishing + Engagement split |
| `CommentService` | Engagement |
| `NotificationService` | Notifications |
| `StoryService` | Stories |
| `ChatService` | Messaging |
| `ChatRealtimeService` | Messaging/Notifications adapter |
| `ReportService` | Moderation & Trust |
| `UserModerationService` | Moderation & Trust |
| `AuditService` | Moderation & Trust |

## 3. Controller -> Module entrypoint mapping
| Current Controller | Primary Module |
|---|---|
| `AuthController` | Identity & Access |
| `ProfilesController` | Identity & Access + Social Graph |
| `PostsController` | Content Publishing + Engagement |
| `CommentsController` | Engagement |
| `CollectionsController` | Content Publishing + Engagement |
| `NotificationsController` | Notifications |
| `StoriesController` | Stories |
| `ChatController` | Messaging |
| `ReportsController` + `Admin*Controller` | Moderation & Trust |

## 4. Query ownership decisions
- `GetFollowersQuery` -> Social Graph (`FollowRelationship`).
- `GetFollowingsQuery` -> Social Graph (`FollowRelationship`).
- `GetProfileByIdQuery` -> Identity & Access (`Profile`).
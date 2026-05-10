# CQRS Command/Query Catalog (Favi-BE)

## 1. Naming conventions
- Commands: `VerbNounCommand`
- Queries: `Get/List/Search...Query`
- Handlers: `<RequestName>Handler`

## 2. Status legend
- ✅ Implemented — handler exists in module project, controller strangled.
- ⏳ Pending — planned in Execution-Checklist, slice noted.
- 🔄 Deferred — decision logged, not planned in current roadmap.

---

## 3. Catalog by module

### 3.1 Identity & Access (`Favi-BE.Modules.Auth`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `RegisterCommand` | ✅ Implemented | |
| `LoginCommand` | ✅ Implemented | |
| `RefreshTokenCommand` | ✅ Implemented | |
| `LogoutCommand` | ✅ Implemented | |
| `ChangePasswordCommand` | ✅ Implemented | |
| `UpdateProfileCommand` | ⏳ Pending — Slice 12 | |
| `DeleteProfileCommand` | ⏳ Pending — Slice 12 | Cascade soft-delete profile + related data |
| `UploadAvatarCommand` | ⏳ Pending — Slice 12 | Includes Cloudinary upload; file bytes resolved in API adapter before dispatch |
| `UploadPosterCommand` | ⏳ Pending — Slice 12 | Same pattern as UploadAvatarCommand |
| `SyncProfileCommand` | ⏳ Pending — Slice 12 | Supabase webhook upsert — idempotent: no-op if profile exists, create if not |
| `UpdateLastActiveCommand` | ✅ Implemented | Auth module — used by ChatController + ProfilesController heartbeat |
| `RequestPasswordResetCommand` | 🔄 Deferred | Email/SMTP flow not set up |
| `ResetPasswordCommand` | 🔄 Deferred | Depends on password reset token store |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetCurrentUserQuery` | ✅ Implemented | |
| `GetProfileByIdQuery` | ⏳ Pending — Slice 12 | |
| `GetRecommendedProfilesQuery` | ⏳ Pending — Slice 12 | |
| `GetOnlineFriendsQuery` | ⏳ Pending — Slice 12 | |
| `GetProfileAvatarQuery` | ⏳ Pending — Slice 12 | Returns avatar image/URL for profile |
| `GetProfilePosterQuery` | ⏳ Pending — Slice 12 | Returns poster image/URL for profile |
| `GetAuthSessionsQuery` | 🔄 Deferred | Depends on AuthSession table (additive migration not yet applied) |

---

### 3.2 Social Graph (`Favi-BE.Modules.SocialGraph`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `FollowUserCommand` | ✅ Implemented | |
| `UnfollowUserCommand` | ✅ Implemented | |
| `AddSocialLinkCommand` | ✅ Implemented | |
| `RemoveSocialLinkCommand` | ✅ Implemented | |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetFollowersQuery` | ✅ Implemented | |
| `GetFollowingsQuery` | ✅ Implemented | |
| `GetSocialLinksQuery` | ✅ Implemented | |
| `GetFollowSuggestionsQuery` | 🔄 Deferred | Not yet planned in roadmap |

---

### 3.3 Content Publishing (`Favi-BE.Modules.ContentPublishing`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `CreatePostCommand` | ✅ Implemented | |
| `UpdatePostCommand` | ✅ Implemented | |
| `DeletePostCommand` | ✅ Implemented | |
| `ArchivePostCommand` | ✅ Implemented | |
| `AddPostMediaCommand` | ✅ Implemented | |
| `ReorderPostMediaCommand` | ✅ Implemented | |
| `AddPostTagsCommand` | ✅ Implemented | |
| `RemovePostTagCommand` | ✅ Implemented | |
| `CreateCollectionCommand` | ✅ Implemented | |
| `UpdateCollectionCommand` | ✅ Implemented | |
| `DeleteCollectionCommand` | ✅ Implemented | |
| `AddPostToCollectionCommand` | ✅ Implemented | |
| `RemovePostFromCollectionCommand` | ✅ Implemented | |
| `SharePostCommand` | ✅ Implemented | |
| `UnsharePostCommand` | ✅ Implemented | |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetPostByIdQuery` | ⏳ Pending — Slice 10 | |
| `GetNewsFeedQuery` | ⏳ Pending — Slice 10 | High-volume — Dapper candidate (see `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`) |
| `GetGuestFeedQuery` | ⏳ Pending — Slice 10 | |
| `GetExploreFeedQuery` | ⏳ Pending — Slice 10 | |
| `GetLatestFeedQuery` | ⏳ Pending — Slice 10 | |
| `GetProfilePostsQuery` | ⏳ Pending — Slice 10 | |
| `SearchPostsQuery` | ⏳ Pending — Slice 10 | EFCore + vector merge — Dapper candidate |
| `GetArchivedPostsQuery` | ⏳ Pending — Slice 10 | |
| `GetRecycleBinQuery` | ⏳ Pending — Slice 10 | |
| `GetRepostsByProfileQuery` | ⏳ Pending — Slice 10 | |
| `GetRepostByIdQuery` | ⏳ Pending — Slice 10 | |
| `GetCollectionByIdQuery` | ⏳ Pending — Slice 10 | CollectionsController `GET /collections/{id}` |
| `GetCollectionsQuery` | ⏳ Pending — Slice 10 | CollectionsController `GET /collections/owner/{ownerId}` |
| `GetCollectionPostsQuery` | ⏳ Pending — Slice 10 | CollectionsController `GET /collections/{id}/posts` |
| `GetTrendingCollectionsQuery` | ⏳ Pending — Slice 10 | CollectionsController `GET /collections/trending` |
| `GetFeedWithRepostsQuery` | ⏳ Pending — Slice 10 | Feed combining posts + reposts — Dapper candidate |

---

### 3.4 Engagement (`Favi-BE.Modules.Engagement`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `CreateCommentCommand` | ✅ Implemented | |
| `UpdateCommentCommand` | ✅ Implemented | |
| `DeleteCommentCommand` | ✅ Implemented | |
| `TogglePostReactionCommand` | ✅ Implemented | |
| `ToggleCommentReactionCommand` | ✅ Implemented | |
| `ToggleCollectionReactionCommand` | ✅ Implemented | |
| `ToggleRepostReactionCommand` | ✅ Implemented | |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetCommentsByPostQuery` | ✅ Implemented | |
| `GetCommentByIdQuery` | ✅ Implemented | |
| `GetPostReactionsQuery` | ✅ Implemented | |
| `GetCommentReactionsQuery` | ✅ Implemented | |
| `GetCollectionReactionsQuery` | ✅ Implemented | |
| `GetPostReactorsQuery` | ✅ Implemented | |
| `GetCommentReactorsQuery` | ✅ Implemented | |
| `GetCollectionReactorsQuery` | ✅ Implemented | |

---

### 3.5 Notifications (`Favi-BE.Modules.Notifications`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `CreateNotificationCommand` | ✅ Implemented (internal/event-driven) | Not a MediatR command — handled by `IInboxConsumer` implementations (`UserFollowedNotificationConsumer`, `CommentCreatedNotificationConsumer`, `PostReactionToggledNotificationConsumer`, `CommentReactionToggledNotificationConsumer`). Triggered via Outbox → OutboxProcessor. |
| `MarkNotificationAsReadCommand` | ⏳ Pending — Slice 11 | Currently handled by `INotificationService.MarkAsReadAsync` in `NotificationsController` |
| `MarkAllNotificationsAsReadCommand` | ⏳ Pending — Slice 11 | Currently handled by `INotificationService.MarkAllAsReadAsync` in `NotificationsController` |
| `DeleteNotificationCommand` | ⏳ Pending — Slice 11 | Currently handled by `INotificationService.DeleteNotificationAsync` in `NotificationsController` |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetNotificationsQuery` | ⏳ Pending — Slice 11 | Dapper candidate (see `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`) |
| `GetUnreadNotificationCountQuery` | ⏳ Pending — Slice 11 | Dapper candidate |

---

### 3.6 Stories (`Favi-BE.Modules.Stories`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `CreateStoryCommand` | ✅ Implemented | |
| `ArchiveStoryCommand` | ✅ Implemented | |
| `DeleteStoryCommand` | ✅ Implemented | |
| `RecordStoryViewCommand` | ✅ Implemented | |
| `ExpireStoryCommand` | ✅ Implemented | Internal/background — dispatched by `StoryExpirationService` hosted service |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetStoryByIdQuery` | ✅ Implemented | StoriesController strangled |
| `GetViewableStoriesQuery` | ✅ Implemented | StoriesController strangled |
| `GetActiveStoriesByProfileQuery` | ✅ Implemented | Absorbs profile existence guard |
| `GetArchivedStoriesQuery` | ✅ Implemented | StoriesController strangled |
| `GetStoryViewersQuery` | ✅ Implemented | StoriesController strangled |
| `GetActiveStoryCountQuery` | ✅ Implemented | StoriesController strangled |

---

### 3.7 Messaging (`Favi-BE.Modules.Messaging`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `GetOrCreateDmCommand` | ✅ Implemented | |
| `CreateGroupConversationCommand` | ✅ Implemented | |
| `SendMessageCommand` | ✅ Implemented | |
| `MarkConversationReadCommand` | ✅ Implemented | |
| `UpdateLastActiveCommand` | ✅ Implemented | |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetConversationsQuery` | ✅ Implemented | Dapper candidate (see `ReadWrite-Segregation-EFCore-To-Dapper-Plan.md`) |
| `GetMessagesQuery` | ✅ Implemented | Dapper candidate |
| `GetUnreadMessagesCountQuery` | ✅ Implemented | |

---

### 3.8 Moderation & Trust (`Favi-BE.Modules.Moderation`)

**Commands**

| Command | Status | Notes |
|---|---|---|
| `CreateReportCommand` | ✅ Implemented | |
| `ResolveReportCommand` | ✅ Implemented | |
| `ModerateUserCommand` | ✅ Implemented | |
| `RevokeModerationCommand` | ✅ Implemented | |
| `LogAdminActionCommand` | ✅ Implemented | |

**Queries**

| Query | Status | Notes |
|---|---|---|
| `GetReportsQuery` | ✅ Implemented | |
| `GetReportByIdQuery` | ✅ Implemented | |
| `GetUserModerationHistoryQuery` | ✅ Implemented | |
| `GetAdminActionAuditQuery` | ✅ Implemented | |

---

## 4. Handler policy
- Command handler: dùng write repository/UoW, tracking enabled. Không gọi `I<Module>QueryReader`.
- Query handler: dùng `I<Module>QueryReader` với `AsNoTracking`, projection DTO. Không mutate state.
- Command handler không gọi query reader.

## 5. Authorization touchpoints
- Identity commands require authenticated context, trừ `RegisterCommand`/`LoginCommand`.
- Admin commands (`ModerateUserCommand`, `LogAdminActionCommand`, `ResolveReportCommand`, `RevokeModerationCommand`) require admin policy.
- Notification read/delete (`MarkNotificationAsReadCommand`, `DeleteNotificationCommand`, `GetNotificationsQuery`) kiểm tra owner of notification.
- Social graph commands kiểm tra self-follow/permission policy (`FollowUserCommand`).

# CQRS Command/Query Catalog (Favi-BE)

## 1. Naming conventions
- Commands: `VerbNounCommand`
- Queries: `Get/List/Search...Query`
- Handlers: `<RequestName>Handler`

## 2. Catalog by module

### 2.1 Identity & Access
**Commands**
- `RegisterCommand`
- `LoginCommand`
- `RefreshTokenCommand`
- `LogoutCommand`
- `ChangePasswordCommand`
- `RequestPasswordResetCommand` (optional)
- `ResetPasswordCommand` (optional)
- `UpdateProfileCommand`

**Queries**
- `GetCurrentUserQuery`
- `GetProfileByIdQuery`
- `GetRecommendedProfilesQuery`
- `GetOnlineFriendsQuery`
- `GetAuthSessionsQuery` (if session tracking enabled)

### 2.2 Social Graph
**Commands**
- `FollowUserCommand`
- `UnfollowUserCommand`
- `AddSocialLinkCommand`
- `RemoveSocialLinkCommand`

**Queries**
- `GetFollowersQuery`
- `GetFollowingsQuery`
- `GetSocialLinksQuery`
- `GetFollowSuggestionsQuery`

### 2.3 Content Publishing
**Commands**
- `CreatePostCommand`, `UpdatePostCommand`, `DeletePostCommand`, `ArchivePostCommand`
- `AddPostMediaCommand`, `ReorderPostMediaCommand`
- `AddPostTagsCommand`, `RemovePostTagCommand`
- `CreateCollectionCommand`, `UpdateCollectionCommand`, `DeleteCollectionCommand`
- `AddPostToCollectionCommand`, `RemovePostFromCollectionCommand`
- `SharePostCommand`, `UnsharePostCommand`

**Queries**
- `GetPostByIdQuery`
- `GetNewsFeedQuery`
- `GetProfilePostsQuery`
- `SearchPostsQuery`
- `GetCollectionByIdQuery`
- `GetCollectionsQuery`
- `GetRepostsByProfileQuery`

### 2.4 Engagement
**Commands**
- `CreateCommentCommand`, `UpdateCommentCommand`, `DeleteCommentCommand`
- `TogglePostReactionCommand`, `ToggleCommentReactionCommand`
- `ToggleCollectionReactionCommand`, `ToggleRepostReactionCommand`

**Queries**
- `GetCommentsByPostQuery`
- `GetCommentByIdQuery`
- `GetPostReactionsQuery`, `GetCommentReactionsQuery`, `GetCollectionReactionsQuery`
- `GetPostReactorsQuery`, `GetCommentReactorsQuery`, `GetCollectionReactorsQuery`

### 2.5 Notifications
**Commands**
- `CreateNotificationCommand` (internal/event-driven)
- `MarkNotificationAsReadCommand`
- `MarkAllNotificationsAsReadCommand`
- `DeleteNotificationCommand`

**Queries**
- `GetNotificationsQuery`
- `GetUnreadNotificationCountQuery`

### 2.6 Stories
**Commands**
- `CreateStoryCommand`, `ArchiveStoryCommand`, `DeleteStoryCommand`
- `RecordStoryViewCommand`
- `ExpireStoryCommand` (internal/background)

**Queries**
- `GetStoryByIdQuery`
- `GetViewableStoriesQuery`
- `GetActiveStoriesByProfileQuery`
- `GetArchivedStoriesQuery`
- `GetStoryViewersQuery`

### 2.7 Messaging
**Commands**
- `GetOrCreateDmCommand`
- `CreateGroupConversationCommand`
- `SendMessageCommand`
- `MarkConversationReadCommand`
- `UpdateLastActiveCommand`

**Queries**
- `GetConversationsQuery`
- `GetMessagesQuery`
- `GetUnreadMessagesCountQuery`

### 2.8 Moderation & Trust
**Commands**
- `CreateReportCommand`
- `ResolveReportCommand`
- `ModerateUserCommand`
- `RevokeModerationCommand`
- `LogAdminActionCommand`

**Queries**
- `GetReportsQuery`
- `GetReportByIdQuery`
- `GetUserModerationHistoryQuery`
- `GetAdminActionAuditQuery`

## 3. Handler policy
- Command handler: dùng write repository/UoW, tracking enabled.
- Query handler: dùng query reader, no tracking.
- Command handler không gọi query reader.

## 4. Authorization touchpoints
- Identity commands require authenticated context, trừ `RegisterCommand`/`LoginCommand`.
- Admin commands require admin policy.
- Notification read/delete kiểm tra owner of notification.
- Social graph commands kiểm tra self-follow/permission policy.
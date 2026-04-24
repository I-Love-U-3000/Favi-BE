# Schema Transition Plan (Expand -> Migrate -> Switch -> Contract)

## 1. New foundational tables
### 1.1 `OutboxMessages`
- `Id` (uuid, pk)
- `OccurredOnUtc` (timestamptz)
- `Type` (text)
- `Payload` (jsonb)
- `CorrelationId` (text nullable)
- `CausationId` (text nullable)
- `Status` (text)
- `Retries` (int)
- `ProcessedOnUtc` (timestamptz nullable)
- `Error` (text nullable)

Indexes:
- `(Status, OccurredOnUtc)`
- `(ProcessedOnUtc)`

### 1.2 `InboxMessages`
- `Id` (uuid, pk)
- `ReceivedOnUtc` (timestamptz)
- `Type` (text)
- `Payload` (jsonb)
- `MessageId` (text)
- `Consumer` (text)
- `Status` (text)
- `Retries` (int)
- `ProcessedOnUtc` (timestamptz nullable)
- `Error` (text nullable)

Unique index:
- `(MessageId, Consumer)` for dedup.

### 1.3 `AuthSessions` (new)
- `Id` (uuid)
- `ProfileId` (uuid)
- `RefreshTokenHash` (text)
- `IssuedAtUtc` (timestamptz)
- `ExpiresAtUtc` (timestamptz)
- `RevokedAtUtc` (timestamptz nullable)
- `RevokedReason` (text nullable)

## 2. Module table ownership mapping
| Existing Table | Target Owner Module | Notes |
|---|---|---|
| `Profiles`, `EmailAccounts` | Identity & Access | add `AuthSessions` |
| `Follows`, `SocialLinks` | Social Graph | includes followers/followings queries |
| `Posts`, `PostMedias`, `PostTags`, `Tags`, `Collections`, `PostCollections`, `Reposts` | Content Publishing | keep additive projection tables |
| `Comments`, `Reactions` | Engagement | per-target idempotency checks |
| `Notifications` | Notifications | add unread projection if needed |
| `Stories`, `StoryViews` | Stories | expiry path |
| `Conversations`, `Messages`, `UserConversations`, `MessageReads` | Messaging | read/write split later |
| `Reports`, `UserModerations`, `AdminActions` | Moderation & Trust | audit integrity |

## 3. Execution phases
### 3.1 Expand
- Add `OutboxMessages`, `InboxMessages`, `AuthSessions`.
- Add nullable columns/indexes needed for projections.

### 3.2 Migrate/Backfill
- Backfill session records where needed.
- Backfill notification read counters/projections if added.
- Run row count + checksum checks.

### 3.3 Switch
- Switch handlers to new read contracts.
- Keep legacy query path as fallback until parity pass.

### 3.4 Contract
- Remove deprecated columns/tables only after parity + stability window.

## 4. Rollback strategy
- Every migration step reversible by dedicated down migration.
- If parity mismatch or error rate spike: `git revert` + rollback DB migration to prior stable snapshot.
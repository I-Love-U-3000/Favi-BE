# 08 — Error Handling, Standard Error Envelope & Error Codes (Favi-BE)

## 1. Overview & Architectural Principles

In a distributed, modular social media platform, predictable error handling is essential for system stability, debugging, client UI resilience, and end-to-end traceability. Favi-BE enforces a standardized, machine-readable error communication contract across all Bounded Contexts, Controllers, and Middleware layers.

### Key Architectural Guidelines
1. **Explicit, Typed Error Contracts**: Errors are never returned as arbitrary unstructured strings. Every business failure returns a standardized error envelope containing a machine-readable `code`.
2. **Layered Separation of Failure Concerns**:
   - **Domain Layer**: Enforces invariants via `IBusinessRule` and throws `BusinessRuleValidationException`.
   - **Application Layer**: Validates request formats via MediatR `ValidationBehavior` (FluentValidation) and expresses expected operational failures via typed result objects (e.g., `CommandResult.Fail(code, message)`).
   - **Infrastructure & Middleware Layer**: Intercepts cross-cutting failures (Idempotency conflicts, database concurrency tokens, unhandled exceptions) and translates them into uniform HTTP responses.
3. **No Sensitive Leaks**: Stack traces and raw internal database exceptions are never exposed to public clients in production.

---

## 2. Standard Error Response Envelope

Every failed HTTP response from Favi-BE conforms to the following JSON structure:

```json
{
  "statusCode": 400,
  "code": "POST_NOT_FOUND",
  "message": "The specified post does not exist or has been deleted.",
  "details": "Optional technical context, broken rule type, or field-level validation errors"
}
```

### Field Definitions

| Field | Type | Description |
|---|---|---|
| `statusCode` | `integer` | The HTTP status code (e.g., `400`, `401`, `403`, `404`, `409`, `429`, `500`). |
| `code` | `string` | Machine-readable, uppercase alphanumeric identifier (e.g., `CONCURRENCY_CONFLICT`, `FORBIDDEN`). |
| `message` | `string` | User-facing, localized explanation suitable for client UI display. |
| `details` | `string` *(optional)* | Diagnostic details, broken domain rule type name, or aggregated field validation failures. |

---

## 3. End-to-End Error Handling Pipeline

```
Incoming Request
      │
      ▼
[ NGINX Gateway ] ──────────────────▶ Exceeds rate limit? ──▶ 429 Too Many Requests
      │
      ▼
[ IdempotencyMiddleware ] ──────────▶ Duplicate key in flight? ──▶ 409 Conflict (IDEMPOTENT_OPERATION_IN_PROGRESS)
      │                               Missing / invalid key?  ──▶ 400 Bad Request (MISSING/INVALID_IDEMPOTENCY_KEY)
      ▼
[ ExceptionHandlingMiddleware ] (Global try/catch wrapper)
      │
      ▼
[ MediatR ValidationBehavior ] ─────▶ FluentValidation failed? ──▶ 400 Bad Request (ValidationException)
      │
      ▼
[ Command / Query Handler ]
      │
      ├──▶ Domain invariant broken? ──▶ Throws BusinessRuleValidationException
      │                                  │
      │                                  ▼
      │                     [ ExceptionHandlingMiddleware ] ──▶ 400 Bad Request (BUSINESS_RULE_VIOLATION)
      │
      ├──▶ Concurrency conflict? ────▶ Returns CommandResult.Fail("CONCURRENCY_CONFLICT", ...)
      │                                  │
      │                                  ▼
      │                             [ Controller ] ─────────────▶ 409 Conflict
      │
      ├──▶ Resource missing/denied? ─▶ Returns CommandResult.Fail("RESOURCE_NOT_FOUND", ...)
      │                                  │
      │                                  ▼
      │                             [ Controller ] ─────────────▶ 404 NotFound / 403 Forbidden
      │
      └──▶ Unhandled Exception ──────▶ Throws Exception
                                         │
                                         ▼
                            [ ExceptionHandlingMiddleware ] ──▶ 500 InternalServerError (INTERNAL_SERVER_ERROR)
```

---

## 4. Comprehensive Error Codes Catalog

### 4.1 Global Middleware & Infrastructure Codes

| Error Code | HTTP Status | Origin / Handler | Description & Trigger Condition |
|---|:---:|---|---|
| `INTERNAL_SERVER_ERROR` | 500 | `ExceptionHandlingMiddleware` | Unhandled exception caught at the top-level request pipeline. |
| `BUSINESS_RULE_VIOLATION` | 400 | `ExceptionHandlingMiddleware` | Domain aggregate invariant violation thrown via `BusinessRuleValidationException`. |
| `MISSING_IDEMPOTENCY_KEY` | 400 | `IdempotencyMiddleware` | State-mutating request under `/api/` omitted the mandatory `Idempotency-Key` header. |
| `INVALID_IDEMPOTENCY_KEY` | 400 | `IdempotencyMiddleware` | `Idempotency-Key` header value exceeds the 128-character limit. |
| `IDEMPOTENT_OPERATION_IN_PROGRESS` | 409 | `IdempotencyMiddleware` | A request with the same idempotency key is currently executing on another worker thread. |
| `CONCURRENCY_CONFLICT` | 409 | Command Handlers & Controllers | The entity's `Version` does not match the version held in the database (optimistic locking failure). |

---

### 4.2 Content Publishing Module (`Favi-BE.Modules.ContentPublishing`)

| Error Code | HTTP Status | Triggering Commands | Description |
|---|:---:|---|---|
| `POST_NOT_FOUND` | 404 | `UpdatePost`, `DeletePost`, `ArchivePost`, `RestorePost`, `PermanentDeletePost`, `SharePost` | The specified post ID does not exist in the database. |
| `POST_DELETED` | 400 | `ArchivePost` | Cannot archive a post that is already soft-deleted. |
| `POST_NOT_DELETED` | 400 | `RestorePost` | Cannot restore a post that is not currently soft-deleted. |
| `POST_FORBIDDEN_OR_NOT_FOUND` | 403 | `UpdatePost`, `DeletePost` | The caller is not the author of the post or lacks moderation permissions. |
| `POST_RESTORE_FAILED` | 403 | `RestorePost` | The caller is unauthorized to restore the specified post. |
| `POST_PERMANENT_DELETE_FAILED` | 403 | `PermanentDeletePost` | The caller is unauthorized to permanently purge the post. |
| `POST_ARCHIVE_FAILED` | 403 | `ArchivePost` | The caller is unauthorized to archive the post. |
| `COLLECTION_NOT_FOUND` | 404 | `UpdateCollection`, `DeleteCollection`, `AddPostToCollection`, `RemovePostFromCollection` | Target collection does not exist. |
| `FORBIDDEN` | 403 | `AddPostToCollection`, `RemovePostFromCollection`, `ReorderPostMedia`, `AddPostTags`, `RemovePostTag` | Caller lacks ownership or mutation rights on the target resource. |
| `REPOST_NOT_FOUND` | 404 | `UnsharePost` | The repost record does not exist or has already been removed. |

---

### 4.3 Engagement Module (`Favi-BE.Modules.Engagement`)

| Error Code | HTTP Status | Triggering Commands | Description |
|---|:---:|---|---|
| `COMMENT_NOT_FOUND` | 404 | `UpdateComment`, `ToggleCommentReaction` | Target comment ID does not exist. |
| `COMMENT_FORBIDDEN` | 403 | `UpdateComment` | Caller is not the author of the comment. |
| `COMMENT_NOT_FOUND_OR_FORBIDDEN` | 403 / 404 | `DeleteComment` | Comment does not exist or caller lacks rights to delete it. |
| `POST_NOT_FOUND` | 404 | `CreateComment`, `TogglePostReaction` | Target post for comment or reaction does not exist. |
| `REPOST_NOT_FOUND` | 404 | `ToggleRepostReaction` | Target repost for reaction does not exist. |
| `COLLECTION_NOT_FOUND` | 404 | `ToggleCollectionReaction` | Target collection for reaction does not exist. |

---

### 4.4 Identity & Access Module (`Favi-BE.Modules.Auth`)

| Error Code | HTTP Status | Triggering Commands | Description |
|---|:---:|---|---|
| `PROFILE_NOT_FOUND` | 404 | `UpdateProfileCommand` | Target user profile does not exist. |
| `CONCURRENCY_CONFLICT` | 409 | `UpdateProfileCommand` | Profile version mismatch; another session has updated the user record. |
| `INVALID_CREDENTIALS` | 401 | `LoginCommand` | Provided email or password does not match registered credentials. |
| `USER_BANNED` | 403 | Auth & Profile Handlers | User account is suspended or banned by administration. |

---

### 4.5 Social Graph Module (`Favi-BE.Modules.SocialGraph`)

| Error Code | HTTP Status | Triggering Commands | Description |
|---|:---:|---|---|
| `PROFILE_NOT_FOUND` | 404 | `FollowUserCommand` | Target user to follow does not exist. |
| `NOT_FOLLOWING` | 400 | `UnfollowUserCommand` | Cannot unfollow a user who is not currently followed. |
| `SOCIAL_LINK_NOT_FOUND` | 404 | `RemoveSocialLinkCommand` | Target social link ID does not exist for removal. |

---

### 4.6 Ephemeral Stories Module (`Favi-BE.Modules.Stories`)

| Error Code | HTTP Status | Triggering Commands | Description |
|---|:---:|---|---|
| `STORY_NOT_FOUND` | 404 | `RecordStoryView`, `DeleteStory`, `ArchiveStory`, `ExpireStory` | Target story does not exist or has passed its 24-hour expiration window. |

---

### 4.7 Realtime Messaging Module (`Favi-BE.Modules.Messaging`)

| Error Code | HTTP Status | Triggering Commands | Description |
|---|:---:|---|---|
| `ACCESS_DENIED` | 403 | `MarkConversationReadCommand`, `SendMessageCommand` | Caller is not an active participant in the target conversation. |
| `CONVERSATION_NOT_FOUND` | 404 | Messaging Query Readers & Handlers | Target conversation thread does not exist. |

---

### 4.8 Moderation & Trust Module (`Favi-BE.Modules.Moderation`)

| Error Code | HTTP Status | Triggering Commands | Description |
|---|:---:|---|---|
| `REPORT_NOT_FOUND` | 404 | `ResolveReportCommand` | Target abuse report ticket ID does not exist. |
| `ACTIVE_BAN_NOT_FOUND` | 404 | `RevokeModerationCommand` | Cannot revoke suspension because no active ban was found for the target user. |

---

## 5. Client Consumption & Frontend Integration (Favi-FE)

On the client side, all HTTP requests traverse `src/lib/fetchWrapper.ts`, which extracts and structures the error envelope into an idiomatic TypeScript object:

```typescript
export interface ApiError {
  status: number;
  code?: string;
  message?: string;
  details?: string;
  error?: string;
}
```

### 5.1 Specialized Handling by Error Code

1. **`CONCURRENCY_CONFLICT` (HTTP 409)**:
   - `fetchWrapper` dispatches a global browser event:
     ```typescript
     window.dispatchEvent(new CustomEvent("favi-concurrency-conflict", { detail: thrownError }));
     ```
   - Settings and editor pages subscribe to prompt the user: *"Data was modified in another session. Please reload to view latest changes."*
2. **`auth-token-expired` (HTTP 401)**:
   - When refresh token renewal fails, `fetchWrapper` clears credentials and emits `"auth-token-expired"`, redirecting the user to `/login`.
3. **`429 Too Many Requests`**:
   - The UI displays an alert indicating rate limits have been reached, prompting the user to wait briefly before retrying.

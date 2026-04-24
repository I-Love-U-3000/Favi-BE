# Auth CQRS Catalog

## 1. Scope
Phạm vi auth local + profile identity core.

## 2. Commands
| Command | Input | Output | Notes |
|---|---|---|---|
| `RegisterCommand` | email, password, username, displayName | auth response | tạo `Profile` + `EmailAccount` + optional `AuthSession` |
| `LoginCommand` | emailOrUsername, password | auth response | phát access token + refresh token/session |
| `RefreshTokenCommand` | refresh token/session id | auth response | rotate token, revoke token cũ nếu policy bật |
| `LogoutCommand` | session/token id | success | revoke session/token |
| `ChangePasswordCommand` | currentPassword, newPassword | success | invalidate active sessions theo policy |
| `RequestPasswordResetCommand` | email | success | optional, outbox event for email flow |
| `ResetPasswordCommand` | reset token, newPassword | success | optional |
| `UpdateProfileCommand` | profile fields | profile dto | chỉ owner được update |

## 3. Queries
| Query | Purpose |
|---|---|
| `GetCurrentUserQuery` | lấy user hiện tại theo execution context |
| `GetProfileByIdQuery` | profile detail by id |
| `GetRecommendedProfilesQuery` | profile recommendation hiện tại |
| `GetOnlineFriendsQuery` | online friends metric |
| `GetAuthSessionsQuery` | liệt kê sessions active/revoked (khi thêm `AuthSession`) |

## 4. Session/token lifecycle chuẩn hóa
1. Login tạo `AuthSession` record với `IssuedAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc` nullable.
2. Refresh kiểm tra not revoked + not expired.
3. Logout set `RevokedAtUtc`.
4. Background cleanup xóa session expired theo retention policy.

## 5. Security rules
- Hash password bằng BCrypt/Argon2 (hiện tại BCrypt).
- Never return hash in DTO/log.
- Refresh token rotation: recommended bật ở slice Auth.
- Audit mọi admin-driven auth state changes qua `AdminAction`.
namespace Favi_BE.Modules.Auth.Application.Contracts;

/// <summary>
/// Module-internal user data returned from IAuthWriteRepository lookups.
/// </summary>
public sealed record AuthUserData(
    Guid Id,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    string Role,
    bool IsBanned);

/// <summary>
/// Credential data for password verification.
/// </summary>
public sealed record AuthCredentialData(
    Guid ProfileId,
    string PasswordHash);

/// <summary>
/// Data needed to register a new user.
/// </summary>
public sealed record AuthUserRegistrationData(
    Guid Id,
    string Username,
    string? DisplayName,
    string Email,
    string PasswordHash);

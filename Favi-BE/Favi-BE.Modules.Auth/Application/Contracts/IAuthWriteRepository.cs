namespace Favi_BE.Modules.Auth.Application.Contracts;

/// <summary>
/// Write-side port for the Auth module.
/// Command handlers use this to look up credentials and persist user state.
/// Implementations live in Favi-BE.API as adapters over existing repositories.
/// </summary>
public interface IAuthWriteRepository
{
    /// <summary>Finds user + credential by email. Returns null if not found.</summary>
    Task<(AuthUserData? User, AuthCredentialData? Credential)> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Finds user + credential by username. Returns null if not found.</summary>
    Task<(AuthUserData? User, AuthCredentialData? Credential)> FindByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Finds user by profile ID. Returns null if not found.</summary>
    Task<AuthUserData?> FindUserByIdAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Finds credential (password hash) by profile ID.</summary>
    Task<AuthCredentialData?> FindCredentialByProfileIdAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Returns true if no account exists with this email.</summary>
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default);

    /// <summary>Returns true if no profile exists with this username.</summary>
    Task<bool> IsUsernameUniqueAsync(string username, CancellationToken ct = default);

    /// <summary>Persists a new Profile + EmailAccount atomically.</summary>
    Task RegisterUserAsync(AuthUserRegistrationData data, CancellationToken ct = default);

    /// <summary>Updates the password hash for an existing account.</summary>
    Task UpdatePasswordHashAsync(Guid profileId, string newPasswordHash, CancellationToken ct = default);

    /// <summary>Commits pending changes to the store.</summary>
    Task SaveAsync(CancellationToken ct = default);
}

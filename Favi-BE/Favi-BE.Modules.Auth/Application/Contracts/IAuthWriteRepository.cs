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

    /// <summary>Stamps LastActiveAt = UtcNow for the given profile. Returns the stamped timestamp.</summary>
    Task<DateTime> UpdateLastActiveAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Returns true if a profile with the given id exists.</summary>
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Updates mutable profile fields. Returns false if profile not found.</summary>
    Task<bool> UpdateProfileAsync(Guid profileId, string? username, string? displayName, string? bio,
        string? avatarUrl, string? coverUrl, int? privacyLevel, int? followPrivacyLevel,
        CancellationToken ct = default);

    /// <summary>Deletes the profile and returns false if not found.</summary>
    Task<bool> DeleteProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Idempotent: creates a profile if one does not already exist for the given id.</summary>
    Task CreateProfileIfNotExistsAsync(Guid id, string username, string displayName, CancellationToken ct = default);

    /// <summary>Persists avatar media, replaces any existing avatar. Returns saved media details.</summary>
    Task<(Guid MediaId, string Url, string PublicId, int Width, int Height, string Format, string? ThumbnailUrl)>
        SaveAvatarAsync(Guid profileId, string url, string? thumbnailUrl, string publicId,
            int width, int height, string format, CancellationToken ct = default);

    /// <summary>Persists poster media, replaces any existing poster. Returns saved media details.</summary>
    Task<(Guid MediaId, string Url, string PublicId, int Width, int Height, string Format, string? ThumbnailUrl)>
        SavePosterAsync(Guid profileId, string url, string? thumbnailUrl, string publicId,
            int width, int height, string format, CancellationToken ct = default);

    /// <summary>Commits pending changes to the store.</summary>
    Task SaveAsync(CancellationToken ct = default);
}

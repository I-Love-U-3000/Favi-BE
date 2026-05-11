using Favi_BE.Data;
using Favi_BE.Interfaces;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Auth.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Application.Auth;

/// <summary>
/// Implements IAuthWriteRepository (Auth module port) using the existing
/// IUnitOfWork + AppDbContext infrastructure. Keeps the module free of API dependencies.
/// </summary>
internal sealed class AuthWriteRepositoryAdapter : IAuthWriteRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public AuthWriteRepositoryAdapter(IUnitOfWork uow, AppDbContext db)
    {
        _uow = uow;
        _db = db;
    }

    public async Task<(AuthUserData? User, AuthCredentialData? Credential)> FindByEmailAsync(
        string email, CancellationToken ct = default)
    {
        var emailAccount = await _uow.EmailAccounts.GetByEmailAsync(email);
        if (emailAccount?.Profile is null)
            return (null, null);

        return (MapUser(emailAccount.Profile), new AuthCredentialData(emailAccount.Id, emailAccount.PasswordHash));
    }

    public async Task<(AuthUserData? User, AuthCredentialData? Credential)> FindByUsernameAsync(
        string username, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByUsernameAsync(username);
        if (profile is null)
            return (null, null);

        var emailAccount = await _uow.EmailAccounts.GetByIdAsync(profile.Id);
        if (emailAccount is null)
            return (null, null);

        return (MapUser(profile), new AuthCredentialData(emailAccount.Id, emailAccount.PasswordHash));
    }

    public async Task<AuthUserData?> FindUserByIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        return profile is null ? null : MapUser(profile);
    }

    public async Task<AuthCredentialData?> FindCredentialByProfileIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var emailAccount = await _uow.EmailAccounts.GetByIdAsync(profileId);
        return emailAccount is null
            ? null
            : new AuthCredentialData(emailAccount.Id, emailAccount.PasswordHash);
    }

    public Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default)
        => _uow.EmailAccounts.IsEmailUniqueAsync(email);

    public Task<bool> IsUsernameUniqueAsync(string username, CancellationToken ct = default)
        => _uow.Profiles.IsUsernameUniqueAsync(username);

    public async Task RegisterUserAsync(AuthUserRegistrationData data, CancellationToken ct = default)
    {
        var profile = new Profile
        {
            Id = data.Id,
            Username = data.Username,
            DisplayName = data.DisplayName,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            PrivacyLevel = PrivacyLevel.Public
        };

        var emailAccount = new EmailAccount
        {
            Id = data.Id,
            Email = data.Email,
            PasswordHash = data.PasswordHash,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Profiles.AddAsync(profile);
        await _uow.EmailAccounts.AddAsync(emailAccount);
    }

    public async Task UpdatePasswordHashAsync(Guid profileId, string newPasswordHash, CancellationToken ct = default)
    {
        var emailAccount = await _uow.EmailAccounts.GetByIdAsync(profileId);
        if (emailAccount is null) return;

        emailAccount.PasswordHash = newPasswordHash;
        _uow.EmailAccounts.Update(emailAccount);
    }

    public async Task<DateTime> UpdateLastActiveAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is null)
            throw new ArgumentException($"Profile '{profileId}' not found.");

        profile.LastActiveAt = DateTime.UtcNow;
        _uow.Profiles.Update(profile);
        return profile.LastActiveAt.Value;
    }

    public Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => _db.Profiles.AnyAsync(p => p.Id == profileId, ct);

    public async Task<bool> UpdateProfileAsync(
        Guid profileId, string? username, string? displayName, string? bio,
        string? avatarUrl, string? coverUrl, int? privacyLevel, int? followPrivacyLevel,
        CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is null) return false;

        if (!string.IsNullOrWhiteSpace(username) && username != profile.Username)
            profile.Username = username;
        if (!string.IsNullOrWhiteSpace(displayName))
            profile.DisplayName = displayName;
        if (!string.IsNullOrWhiteSpace(bio))
            profile.Bio = bio;
        if (!string.IsNullOrWhiteSpace(avatarUrl))
            profile.AvatarUrl = avatarUrl;
        if (!string.IsNullOrWhiteSpace(coverUrl))
            profile.CoverUrl = coverUrl;
        if (privacyLevel.HasValue)
            profile.PrivacyLevel = (PrivacyLevel)privacyLevel.Value;
        if (followPrivacyLevel.HasValue)
            profile.FollowPrivacyLevel = (PrivacyLevel)followPrivacyLevel.Value;

        profile.LastActiveAt = DateTime.UtcNow;
        _uow.Profiles.Update(profile);
        return true;
    }

    public async Task<bool> DeleteProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is null) return false;
        _uow.Profiles.Remove(profile);
        return true;
    }

    public async Task CreateProfileIfNotExistsAsync(Guid id, string username, string displayName, CancellationToken ct = default)
    {
        if (await _db.Profiles.AnyAsync(p => p.Id == id, ct)) return;

        var profile = new Profile
        {
            Id = id,
            Username = username,
            DisplayName = displayName,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            PrivacyLevel = PrivacyLevel.Public,
            IsBanned = false
        };
        await _uow.Profiles.AddAsync(profile);
    }

    public async Task<(Guid MediaId, string Url, string PublicId, int Width, int Height, string Format, string? ThumbnailUrl)>
        SaveAvatarAsync(Guid profileId, string url, string? thumbnailUrl, string publicId,
            int width, int height, string format, CancellationToken ct = default)
    {
        var oldAvatar = await _uow.PostMedia.GetProfileAvatar(profileId);
        if (oldAvatar is not null)
            _uow.PostMedia.Remove(oldAvatar);

        var media = new PostMedia
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PostId = null,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            PublicId = publicId,
            Width = width,
            Height = height,
            Format = format,
            Position = 0,
            IsAvatar = true,
            IsPoster = false
        };
        await _uow.PostMedia.AddAsync(media);

        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is not null)
        {
            profile.AvatarUrl = url;
            profile.LastActiveAt = DateTime.UtcNow;
            _uow.Profiles.Update(profile);
        }

        return (media.Id, url, publicId, width, height, format, thumbnailUrl);
    }

    public async Task<(Guid MediaId, string Url, string PublicId, int Width, int Height, string Format, string? ThumbnailUrl)>
        SavePosterAsync(Guid profileId, string url, string? thumbnailUrl, string publicId,
            int width, int height, string format, CancellationToken ct = default)
    {
        var oldPoster = await _uow.PostMedia.GetProfilePoster(profileId);
        if (oldPoster is not null)
            _uow.PostMedia.Remove(oldPoster);

        var media = new PostMedia
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PostId = null,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            PublicId = publicId,
            Width = width,
            Height = height,
            Format = format,
            Position = 0,
            IsAvatar = false,
            IsPoster = true
        };
        await _uow.PostMedia.AddAsync(media);

        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is not null)
        {
            profile.CoverUrl = url;
            profile.LastActiveAt = DateTime.UtcNow;
            _uow.Profiles.Update(profile);
        }

        return (media.Id, url, publicId, width, height, format, thumbnailUrl);
    }

    public async Task SaveAsync(CancellationToken ct = default)
        => await _uow.CompleteAsync();

    private static AuthUserData MapUser(Profile p) =>
        new(p.Id, p.Username, p.DisplayName, p.AvatarUrl, p.Role.ToString().ToLower(), p.IsBanned);
}

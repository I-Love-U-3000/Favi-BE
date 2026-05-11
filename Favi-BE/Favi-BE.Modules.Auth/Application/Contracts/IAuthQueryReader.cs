using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Contracts;

/// <summary>
/// Read-side port for the Auth module.
/// Query handlers use this for AsNoTracking reads. No mutations allowed.
/// </summary>
public interface IAuthQueryReader
{
    Task<CurrentUserDto?> GetCurrentUserAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Returns true if a profile with the given id exists.</summary>
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Returns profile detail. Returns null if not found or viewer cannot access.</summary>
    Task<ProfileReadModel?> GetProfileByIdAsync(Guid profileId, Guid? viewerId, CancellationToken ct = default);

    Task<IReadOnlyList<ProfileReadModel>> GetRecommendedProfilesAsync(Guid viewerId, int skip, int take, CancellationToken ct = default);

    Task<IReadOnlyList<ProfileReadModel>> GetOnlineFriendsAsync(Guid profileId, int withinLastMinutes, CancellationToken ct = default);

    /// <summary>Returns the avatar URL for the given profile, or null if no avatar exists.</summary>
    Task<string?> GetAvatarUrlAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Returns the poster/cover URL for the given profile, or null if no poster exists.</summary>
    Task<string?> GetPosterUrlAsync(Guid profileId, CancellationToken ct = default);
}

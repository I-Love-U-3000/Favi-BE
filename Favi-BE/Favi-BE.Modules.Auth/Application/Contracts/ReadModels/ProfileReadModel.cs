namespace Favi_BE.Modules.Auth.Application.Contracts.ReadModels;

/// <summary>
/// Pure read model for profile data. PrivacyLevel values are int (0=Public, 1=Followers, 2=Private).
/// </summary>
public sealed record ProfileReadModel(
    Guid Id,
    string Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    string? Email,
    DateTime CreatedAt,
    DateTime LastActiveAt,
    int PrivacyLevel,
    int FollowPrivacyLevel,
    bool IsBanned,
    DateTime? BannedUntil,
    int FollowersCount,
    int FollowingCount
);

using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record ProfileResponse(
        Guid Id,
        string Username,
        string? DisplayName,
        string? Bio,
        string? AvatarUrl,
        string? CoverUrl,
        string? Email,
        DateTime CreatedAt,
        DateTime LastActiveAt,
        PrivacyLevel PrivacyLevel,
        PrivacyLevel FollowPrivacyLevel,
        bool IsBanned,
        DateTime? BannedUntil,
        int? FollowersCount,
        int? FollowingCount,
        int? MutualFriendsCount = null,
        string? RecommendationReason = null,
        int Version = 1
    );

    public record ProfileUpdateRequest(
        string? Username,
        string? DisplayName,
        string? Bio,
        string? AvatarUrl,
        string? CoverUrl,
        PrivacyLevel? PrivacyLevel,
        PrivacyLevel? FollowPrivacyLevel,
        int? Version = null
    );

    public record SocialLinkDto(
        Guid? Id,
        SocialKind SocialKind,
        string Url
    );

    public record FollowResponse(
        Guid FollowerId,
        Guid FolloweeId,
        DateTime CreatedAt,
        string? Username = null,
        string? DisplayName = null,
        string? AvatarUrl = null,
        string? Bio = null
    );
}

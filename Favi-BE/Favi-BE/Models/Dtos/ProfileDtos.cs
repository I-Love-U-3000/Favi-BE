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
        DateTime CreatedAt,
        DateTime LastActiveAt,
        int FollowersCount,
        int FollowingCount
    );

    public record ProfileUpdateRequest(
        string? Username,
        string? DisplayName,
        string? Bio,
        string? AvatarUrl,
        string? CoverUrl
    );

    public record SocialLinkDto(
        Guid? Id,
        SocialKind SocialKind,
        string Url
    );

    public record FollowResponse(
        Guid FollowerId,
        Guid FolloweeId,
        DateTime CreatedAt
    );
}

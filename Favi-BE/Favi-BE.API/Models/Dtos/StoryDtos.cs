using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    // Request DTOs
    public record CreateStoryRequest(
        PrivacyLevel PrivacyLevel
    );

    // Response DTOs
    public record StoryResponse(
        Guid Id,
        Guid ProfileId,
        string ProfileUsername,
        string? ProfileAvatarUrl,
        string MediaUrl,
        string? ThumbnailUrl,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        PrivacyLevel Privacy,
        bool IsArchived,
        bool IsNSFW,
        int ViewCount,
        bool HasViewed
    );

    public record StoryViewerResponse(
        Guid ViewerId,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        DateTime ViewedAt
    );

    // Feed response - grouped by profile
    public record StoryFeedResponse(
        Guid ProfileId,
        string ProfileUsername,
        string? ProfileAvatarUrl,
        IEnumerable<StoryResponse> Stories
    );
}

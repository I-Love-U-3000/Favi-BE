using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record DashboardStatsResponse(
        int TotalUsers,
        int TotalPosts,
        int ActiveUsers,
        int BannedUsers,
        int PendingReports,
        int TodayPosts,
        int TodayUsers
    );

    public record AnalyticsUserDto(
        Guid Id,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        string? Email,
        DateTime CreatedAt,
        DateTime LastActiveAt,
        bool IsBanned,
        DateTime? BannedUntil,
        UserRole Role,
        int PostsCount,
        int FollowersCount
    );

    public record AnalyticsPostDto(
        Guid Id,
        Guid AuthorProfileId,
        string? AuthorUsername,
        string? AuthorDisplayName,
        string? AuthorAvatarUrl,
        string? Caption,
        string? ThumbnailUrl,
        DateTime CreatedAt,
        PrivacyLevel PrivacyLevel,
        int CommentsCount,
        int ReactionsCount,
        bool IsDeleted
    );

    public record AnalyticsQueryParameters(
        string? Search,
        string? Role,
        string? Status,
        string? SortBy,
        string? SortOrder,
        int Page = 1,
        int PageSize = 20
    );

    public record AnalyticsCommentDto(
        Guid Id,
        string Content,
        Guid PostId,
        AnalyticsCommentPostDto? Post,
        AnalyticsCommentAuthorDto Author,
        Guid? ParentId,
        int LikeCount,
        int ReplyCount,
        string Status,
        DateTime CreatedAt
    );

    public record AnalyticsCommentPostDto(
        Guid Id,
        string? Caption,
        AnalyticsCommentAuthorDto? Author
    );

    public record AnalyticsCommentAuthorDto(
        Guid Id,
        string Username,
        string? DisplayName,
        string? Avatar
    );

    public record CommentStatsDto(
        int Total,
        int Deleted,
        int Hidden,
        int Active
    );
}

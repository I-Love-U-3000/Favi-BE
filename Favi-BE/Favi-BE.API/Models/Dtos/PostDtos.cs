using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public enum FeedItemType
    {
        Post,
        Repost
    }

    public record FeedItemDto(
        FeedItemType Type,
        PostResponse? Post,
        RepostResponse? Repost,
        DateTime CreatedAt  // For sorting
    );

    public record CreatePostRequest(
        // Guid AuthorProfileId lấy từ jwt claims tránh mạo danh đăng
        string? Caption,
        IEnumerable<String>? Tags,
        PrivacyLevel PrivacyLevel,   
        LocationDto? Location
    );

    public record UpdatePostRequest(
        string? Caption
    );

    // Response
    public record PostResponse(
        Guid Id,
        Guid AuthorProfileId,
        string? Caption,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        PrivacyLevel PrivacyLevel,
        IEnumerable<PostMediaResponse> Medias,
        IEnumerable<TagDto> Tags,
        ReactionSummaryDto Reactions,
        int CommentsCount,
        LocationDto? Location,
        bool IsNSFW
    );

    public record PostMediaResponse(
        Guid Id,
        Guid PostId,
        string Url,
        string PublicId, //currently used for Cloudinary
        int Width,
        int Height,
        string Format,
        int Position,
        string? ThumbnailUrl
    );

    public record ReactionSummaryDto(
        int Total,
        Dictionary<ReactionType, int> ByType,
        ReactionType? CurrentUserReaction
    );

    public record ToggleReactionRequest(
        Guid UserProfileId,
        ReactionType Type
    );

    public record TagDto(
        Guid Id,
        string Name
    );

    public record LocationDto(
        string? Name,
        string? FullAddress,
        double? Latitude,
        double? Longitude
    );

    public record PostReactorResponse(
        Guid ProfileId,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        ReactionType ReactionType,
        DateTime CreatedAt
    );

    public record CollectionReactorResponse(
        Guid ProfileId,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        ReactionType ReactionType,
        DateTime CreatedAt
    );

    // Repost/Share related DTOs
    public record CreateRepostRequest(
        string? Caption  // Optional comment from the sharer
    );

    public record RepostResponse(
        Guid Id,
        Guid ProfileId,  // User who shared the post
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        Guid OriginalPostId,
        string? OriginalCaption,
        Guid OriginalAuthorProfileId,
        string OriginalAuthorUsername,
        string? OriginalAuthorDisplayName,
        string? OriginalAuthorAvatarUrl,
        IEnumerable<PostMediaResponse> OriginalPostMedias,
        string? Caption,  // Sharer's comment
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int CommentsCount,  // Comments on this repost (separate from original post)
        ReactionSummaryDto Reactions,  // Reactions on this repost (separate from original post)
        int RepostsCount,  // Total reposts of the original post
        bool IsRepostedByCurrentUser  // Whether current user has also reposted this
    );
}

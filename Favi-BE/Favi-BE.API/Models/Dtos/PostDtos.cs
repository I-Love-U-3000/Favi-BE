using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record CreatePostRequest(
        // Guid AuthorProfileId lấy từ jwt claims tránh mạo danh đăng
        string? Caption,
        IEnumerable<String>? Tags
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
        int CommentsCount
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
}

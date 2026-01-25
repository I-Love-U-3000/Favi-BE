using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record CreateCommentRequest(
        Guid PostId,
        Guid AuthorProfileId,
        string Content,
        string? MediaUrl,
        Guid? ParentCommentId
    );

    public record UpdateCommentRequest(
        string Content
    );

    public record CommentResponse(
        Guid Id,
        Guid PostId,
        Guid AuthorProfileId,
        string Content,
        string? MediaUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid? ParentCommentId,
        ReactionSummaryDto Reactions
    )
    {
        public List<CommentResponse> Replies { get; init; } = new();
    }

    public record CommentReactorResponse(
        Guid ProfileId,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        ReactionType ReactionType,
        DateTime CreatedAt
    );
}

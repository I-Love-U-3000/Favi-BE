using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record CreateCommentRequest(
        Guid PostId,
        Guid AuthorProfileId,
        string Content,
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
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid? ParentCommentId,
        ReactionSummaryDto Reactions
    )
    {
        public List<CommentResponse> Replies { get; init; } = new();
    }
}

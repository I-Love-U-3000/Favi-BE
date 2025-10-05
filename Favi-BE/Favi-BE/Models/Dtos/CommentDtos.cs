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
        Guid? ParentCommentId // nếu hỗ trợ reply nhiều cấp
    );
}

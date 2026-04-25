namespace Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;

public sealed record CommentWriteData(
    Guid Id,
    Guid PostId,
    Guid? RepostId,
    Guid ProfileId,
    string Content,
    string? MediaUrl,
    Guid? ParentCommentId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

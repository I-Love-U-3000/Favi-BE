namespace Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

public sealed record CommentQueryDto(
    Guid Id,
    Guid PostId,
    Guid? RepostId,
    Guid ProfileId,
    string Content,
    string? MediaUrl,
    Guid? ParentCommentId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ReactionSummaryQueryDto Reactions);

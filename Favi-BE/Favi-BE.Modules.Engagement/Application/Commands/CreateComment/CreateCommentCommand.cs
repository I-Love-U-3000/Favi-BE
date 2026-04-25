using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Responses;

namespace Favi_BE.Modules.Engagement.Application.Commands.CreateComment;

public sealed record CreateCommentCommand(
    Guid PostId,
    Guid? RepostId,
    Guid AuthorId,
    string Content,
    string? MediaUrl,
    Guid? ParentCommentId) : ICommand<CommentCommandResult>;

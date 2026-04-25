using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Responses;

namespace Favi_BE.Modules.Engagement.Application.Commands.UpdateComment;

public sealed record UpdateCommentCommand(
    Guid CommentId,
    Guid RequesterId,
    string Content) : ICommand<CommentCommandResult>;

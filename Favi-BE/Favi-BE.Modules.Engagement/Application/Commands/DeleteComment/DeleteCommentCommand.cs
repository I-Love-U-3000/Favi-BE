using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Responses;

namespace Favi_BE.Modules.Engagement.Application.Commands.DeleteComment;

public sealed record DeleteCommentCommand(
    Guid CommentId,
    Guid RequesterId) : ICommand<EngagementCommandResult>;

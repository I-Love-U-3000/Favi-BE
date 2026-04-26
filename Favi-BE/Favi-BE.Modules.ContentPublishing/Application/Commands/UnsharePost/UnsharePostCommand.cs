using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.UnsharePost;

public sealed record UnsharePostCommand(
    Guid OriginalPostId,
    Guid SharerId
) : IRequest<RepostCommandResult>;

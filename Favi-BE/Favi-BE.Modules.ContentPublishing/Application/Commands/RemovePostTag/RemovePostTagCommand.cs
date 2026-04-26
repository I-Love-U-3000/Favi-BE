using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.RemovePostTag;

public sealed record RemovePostTagCommand(
    Guid PostId,
    Guid TagId,
    Guid RequesterId
) : IRequest<PostCommandResult>;

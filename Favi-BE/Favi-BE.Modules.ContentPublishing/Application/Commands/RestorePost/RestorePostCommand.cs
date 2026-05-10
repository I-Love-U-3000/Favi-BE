using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.RestorePost;

public sealed record RestorePostCommand(
    Guid PostId,
    Guid RequesterId
) : IRequest<PostCommandResult>;

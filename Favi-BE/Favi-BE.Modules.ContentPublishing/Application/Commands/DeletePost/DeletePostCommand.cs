using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.DeletePost;

public sealed record DeletePostCommand(
    Guid PostId,
    Guid RequesterId
) : IRequest<PostCommandResult>;

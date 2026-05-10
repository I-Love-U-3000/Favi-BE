using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.PermanentDeletePost;

public sealed record PermanentDeletePostCommand(
    Guid PostId,
    Guid RequesterId
) : IRequest<PostCommandResult>;

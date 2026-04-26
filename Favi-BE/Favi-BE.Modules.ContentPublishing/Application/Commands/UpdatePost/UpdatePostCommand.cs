using Favi_BE.Modules.ContentPublishing.Application.Responses;
using Favi_BE.Modules.ContentPublishing.Domain;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.UpdatePost;

public sealed record UpdatePostCommand(
    Guid PostId,
    Guid RequesterId,
    string? Caption,
    PostPrivacy? Privacy
) : IRequest<PostCommandResult>;

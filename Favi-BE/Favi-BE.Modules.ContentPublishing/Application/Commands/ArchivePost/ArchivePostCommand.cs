using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.ArchivePost;

public sealed record ArchivePostCommand(
    Guid PostId,
    Guid RequesterId,
    bool IsArchived
) : IRequest<PostCommandResult>;

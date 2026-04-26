using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.ReorderPostMedia;

public sealed record ReorderPostMediaCommand(
    Guid PostId,
    Guid RequesterId,
    IReadOnlyList<(Guid MediaId, int Position)> Positions
) : IRequest<PostCommandResult>;

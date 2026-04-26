using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.SharePost;

public sealed record SharePostCommand(
    Guid OriginalPostId,
    Guid SharerId,
    string? Caption
) : IRequest<RepostCommandResult>;

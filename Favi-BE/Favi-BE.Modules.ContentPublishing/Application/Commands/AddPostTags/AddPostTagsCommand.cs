using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostTags;

public sealed record AddPostTagsCommand(
    Guid PostId,
    Guid RequesterId,
    IReadOnlyList<string> TagNames
) : IRequest<PostCommandResult>;

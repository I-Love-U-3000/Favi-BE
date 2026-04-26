using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostMedia;

public sealed record AddPostMediaCommand(
    Guid PostId,
    Guid RequesterId,
    IReadOnlyList<UploadedMediaItem> MediaItems
) : IRequest<PostCommandResult>;

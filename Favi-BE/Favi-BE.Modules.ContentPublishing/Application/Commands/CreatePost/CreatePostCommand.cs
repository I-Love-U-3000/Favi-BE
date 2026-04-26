using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using Favi_BE.Modules.ContentPublishing.Domain;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.CreatePost;

public sealed record CreatePostCommand(
    Guid AuthorId,
    string? Caption,
    PostPrivacy Privacy,
    string? LocationName,
    string? LocationFullAddress,
    double? LocationLatitude,
    double? LocationLongitude,
    IReadOnlyList<string>? TagNames,
    IReadOnlyList<UploadedMediaItem>? MediaItems
) : IRequest<PostCommandResult>;

using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostToCollection;

public sealed record AddPostToCollectionCommand(
    Guid CollectionId,
    Guid PostId,
    Guid RequesterId
) : IRequest<CollectionCommandResult>;

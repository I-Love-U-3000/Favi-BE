using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.RemovePostFromCollection;

public sealed record RemovePostFromCollectionCommand(
    Guid CollectionId,
    Guid PostId,
    Guid RequesterId
) : IRequest<CollectionCommandResult>;

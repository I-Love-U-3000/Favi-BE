using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.DeleteCollection;

public sealed record DeleteCollectionCommand(
    Guid CollectionId,
    Guid RequesterId
) : IRequest<CollectionCommandResult>;

using Favi_BE.Modules.ContentPublishing.Application.Responses;
using Favi_BE.Modules.ContentPublishing.Domain;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.UpdateCollection;

public sealed record UpdateCollectionCommand(
    Guid CollectionId,
    Guid RequesterId,
    string? Title,
    string? Description,
    CollectionPrivacy? Privacy,
    string? CoverImageUrl,
    string? CoverImagePublicId
) : IRequest<CollectionCommandResult>;

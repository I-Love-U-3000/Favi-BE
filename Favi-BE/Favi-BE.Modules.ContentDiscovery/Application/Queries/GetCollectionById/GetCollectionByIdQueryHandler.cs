using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetCollectionById;

internal sealed class GetCollectionByIdQueryHandler : IRequestHandler<GetCollectionByIdQuery, CollectionReadModel?>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetCollectionByIdQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<CollectionReadModel?> Handle(GetCollectionByIdQuery request, CancellationToken cancellationToken)
        => _reader.GetCollectionByIdAsync(request.CollectionId, request.ViewerId, cancellationToken);
}

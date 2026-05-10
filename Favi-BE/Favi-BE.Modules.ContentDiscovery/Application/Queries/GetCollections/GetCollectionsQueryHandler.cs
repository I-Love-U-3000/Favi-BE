using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetCollections;

internal sealed class GetCollectionsQueryHandler
    : IRequestHandler<GetCollectionsQuery, (IReadOnlyList<CollectionReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetCollectionsQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)> Handle(
        GetCollectionsQuery request, CancellationToken cancellationToken)
        => _reader.GetCollectionsByOwnerAsync(
            request.OwnerId, request.ViewerId, request.Page, request.PageSize, cancellationToken);
}

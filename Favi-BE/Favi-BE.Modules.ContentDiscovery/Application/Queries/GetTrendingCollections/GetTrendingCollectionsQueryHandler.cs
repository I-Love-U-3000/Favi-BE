using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetTrendingCollections;

internal sealed class GetTrendingCollectionsQueryHandler
    : IRequestHandler<GetTrendingCollectionsQuery, (IReadOnlyList<CollectionReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetTrendingCollectionsQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)> Handle(
        GetTrendingCollectionsQuery request, CancellationToken cancellationToken)
        => _reader.GetTrendingCollectionsAsync(request.ViewerId, request.Page, request.PageSize, cancellationToken);
}

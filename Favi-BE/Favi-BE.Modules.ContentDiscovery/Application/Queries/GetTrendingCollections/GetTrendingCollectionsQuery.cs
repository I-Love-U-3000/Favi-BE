using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetTrendingCollections;

public sealed record GetTrendingCollectionsQuery(
    Guid? ViewerId,
    int Page,
    int PageSize) : IQuery<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)>;

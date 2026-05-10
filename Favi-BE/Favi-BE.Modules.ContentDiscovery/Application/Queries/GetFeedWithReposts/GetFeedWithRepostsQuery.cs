using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetFeedWithReposts;

public sealed record GetFeedWithRepostsQuery(
    Guid UserId,
    int Page,
    int PageSize) : IQuery<(IReadOnlyList<FeedItemReadModel> Items, int TotalCount)>;

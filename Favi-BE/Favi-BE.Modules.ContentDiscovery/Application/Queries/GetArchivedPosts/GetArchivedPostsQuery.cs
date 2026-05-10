using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetArchivedPosts;

public sealed record GetArchivedPostsQuery(
    Guid UserId,
    int Page,
    int PageSize) : IQuery<(IReadOnlyList<PostReadModel> Items, int TotalCount)>;

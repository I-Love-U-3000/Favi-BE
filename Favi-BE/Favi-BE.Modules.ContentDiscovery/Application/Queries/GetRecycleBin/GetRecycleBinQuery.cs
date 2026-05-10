using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRecycleBin;

public sealed record GetRecycleBinQuery(
    Guid UserId,
    int Page,
    int PageSize) : IQuery<(IReadOnlyList<PostReadModel> Items, int TotalCount)>;

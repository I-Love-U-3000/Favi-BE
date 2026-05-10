using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRepostsByProfile;

public sealed record GetRepostsByProfileQuery(
    Guid ProfileId,
    Guid? ViewerId,
    int Page,
    int PageSize) : IQuery<(IReadOnlyList<RepostReadModel> Items, int TotalCount)>;

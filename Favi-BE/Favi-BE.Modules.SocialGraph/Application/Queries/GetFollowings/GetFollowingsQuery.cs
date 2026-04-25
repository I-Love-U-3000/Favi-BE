using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowings;

public sealed record GetFollowingsQuery(
    Guid ProfileId,
    int Skip,
    int Take) : IQuery<IReadOnlyList<FollowQueryDto>>;

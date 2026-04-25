using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetPostReactions;

public sealed record GetPostReactionsQuery(
    Guid PostId,
    Guid? CurrentUserId) : IQuery<ReactionSummaryQueryDto>;

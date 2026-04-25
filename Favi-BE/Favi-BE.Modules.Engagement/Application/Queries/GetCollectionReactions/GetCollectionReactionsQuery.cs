using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCollectionReactions;

public sealed record GetCollectionReactionsQuery(
    Guid CollectionId,
    Guid? CurrentUserId) : IQuery<ReactionSummaryQueryDto>;

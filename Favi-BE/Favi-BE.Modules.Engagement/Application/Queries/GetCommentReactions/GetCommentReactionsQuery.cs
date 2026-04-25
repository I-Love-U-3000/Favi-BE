using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCommentReactions;

public sealed record GetCommentReactionsQuery(
    Guid CommentId,
    Guid? CurrentUserId) : IQuery<ReactionSummaryQueryDto>;

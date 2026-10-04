using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetBatchPostReactions;

public sealed record GetBatchPostReactionsQuery(
    IReadOnlyList<Guid> PostIds,
    Guid? CurrentUserId) : IRequest<IReadOnlyDictionary<Guid, ReactionSummaryQueryDto>>;

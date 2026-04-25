using Favi_BE.Modules.Engagement.Domain;

namespace Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

public sealed record ReactionSummaryQueryDto(
    int Total,
    IReadOnlyDictionary<ReactionType, int> ByType,
    ReactionType? CurrentUserReaction);

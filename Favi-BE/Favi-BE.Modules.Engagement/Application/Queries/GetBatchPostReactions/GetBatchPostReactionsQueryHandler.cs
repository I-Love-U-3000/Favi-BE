using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetBatchPostReactions;

internal sealed class GetBatchPostReactionsQueryHandler
    : IRequestHandler<GetBatchPostReactionsQuery, IReadOnlyDictionary<Guid, ReactionSummaryQueryDto>>
{
    private readonly IEngagementQueryReader _reader;

    public GetBatchPostReactionsQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyDictionary<Guid, ReactionSummaryQueryDto>> Handle(
        GetBatchPostReactionsQuery request, CancellationToken cancellationToken)
        => _reader.GetBatchReactionSummariesForPostsAsync(request.PostIds, request.CurrentUserId, cancellationToken);
}

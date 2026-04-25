using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCollectionReactions;

internal sealed class GetCollectionReactionsQueryHandler : IRequestHandler<GetCollectionReactionsQuery, ReactionSummaryQueryDto>
{
    private readonly IEngagementQueryReader _reader;

    public GetCollectionReactionsQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<ReactionSummaryQueryDto> Handle(GetCollectionReactionsQuery request, CancellationToken cancellationToken)
        => _reader.GetReactionSummaryForCollectionAsync(request.CollectionId, request.CurrentUserId, cancellationToken);
}

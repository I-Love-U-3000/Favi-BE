using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetPostReactions;

internal sealed class GetPostReactionsQueryHandler : IRequestHandler<GetPostReactionsQuery, ReactionSummaryQueryDto>
{
    private readonly IEngagementQueryReader _reader;

    public GetPostReactionsQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<ReactionSummaryQueryDto> Handle(GetPostReactionsQuery request, CancellationToken cancellationToken)
        => _reader.GetReactionSummaryForPostAsync(request.PostId, request.CurrentUserId, cancellationToken);
}

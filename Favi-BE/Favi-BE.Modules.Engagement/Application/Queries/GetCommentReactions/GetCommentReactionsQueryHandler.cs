using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCommentReactions;

internal sealed class GetCommentReactionsQueryHandler : IRequestHandler<GetCommentReactionsQuery, ReactionSummaryQueryDto>
{
    private readonly IEngagementQueryReader _reader;

    public GetCommentReactionsQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<ReactionSummaryQueryDto> Handle(GetCommentReactionsQuery request, CancellationToken cancellationToken)
        => _reader.GetReactionSummaryForCommentAsync(request.CommentId, request.CurrentUserId, cancellationToken);
}

using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCommentsByPost;

internal sealed class GetCommentsByPostQueryHandler
    : IRequestHandler<GetCommentsByPostQuery, (IReadOnlyList<CommentQueryDto> Items, int TotalCount)>
{
    private readonly IEngagementQueryReader _reader;

    public GetCommentsByPostQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<(IReadOnlyList<CommentQueryDto> Items, int TotalCount)> Handle(
        GetCommentsByPostQuery request, CancellationToken cancellationToken)
        => _reader.GetCommentsByPostAsync(request.PostId, request.CurrentUserId, request.Page, request.PageSize, cancellationToken);
}

using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCommentById;

internal sealed class GetCommentByIdQueryHandler : IRequestHandler<GetCommentByIdQuery, CommentQueryDto?>
{
    private readonly IEngagementQueryReader _reader;

    public GetCommentByIdQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<CommentQueryDto?> Handle(GetCommentByIdQuery request, CancellationToken cancellationToken)
        => _reader.GetCommentByIdAsync(request.CommentId, request.CurrentUserId, cancellationToken);
}

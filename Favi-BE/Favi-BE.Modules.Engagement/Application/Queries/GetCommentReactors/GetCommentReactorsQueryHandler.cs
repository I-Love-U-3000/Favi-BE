using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCommentReactors;

internal sealed class GetCommentReactorsQueryHandler : IRequestHandler<GetCommentReactorsQuery, IReadOnlyList<ReactorQueryDto>>
{
    private readonly IEngagementQueryReader _reader;

    public GetCommentReactorsQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<ReactorQueryDto>> Handle(GetCommentReactorsQuery request, CancellationToken cancellationToken)
        => _reader.GetReactorsForCommentAsync(request.CommentId, cancellationToken);
}

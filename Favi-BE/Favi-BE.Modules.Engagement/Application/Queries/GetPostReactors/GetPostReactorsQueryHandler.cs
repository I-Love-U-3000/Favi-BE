using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetPostReactors;

internal sealed class GetPostReactorsQueryHandler : IRequestHandler<GetPostReactorsQuery, IReadOnlyList<ReactorQueryDto>>
{
    private readonly IEngagementQueryReader _reader;

    public GetPostReactorsQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<ReactorQueryDto>> Handle(GetPostReactorsQuery request, CancellationToken cancellationToken)
        => _reader.GetReactorsForPostAsync(request.PostId, cancellationToken);
}

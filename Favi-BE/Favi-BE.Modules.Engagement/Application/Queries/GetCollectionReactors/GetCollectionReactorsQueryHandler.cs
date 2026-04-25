using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCollectionReactors;

internal sealed class GetCollectionReactorsQueryHandler : IRequestHandler<GetCollectionReactorsQuery, IReadOnlyList<ReactorQueryDto>>
{
    private readonly IEngagementQueryReader _reader;

    public GetCollectionReactorsQueryHandler(IEngagementQueryReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<ReactorQueryDto>> Handle(GetCollectionReactorsQuery request, CancellationToken cancellationToken)
        => _reader.GetReactorsForCollectionAsync(request.CollectionId, cancellationToken);
}

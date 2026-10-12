using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetPostsBatch;

internal sealed class GetPostsBatchQueryHandler : IRequestHandler<GetPostsBatchQuery, IReadOnlyList<PostReadModel>>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetPostsBatchQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<IReadOnlyList<PostReadModel>> Handle(GetPostsBatchQuery request, CancellationToken cancellationToken)
        => _reader.GetPostsByIdsAsync(request.PostIds, request.ViewerId, cancellationToken);
}

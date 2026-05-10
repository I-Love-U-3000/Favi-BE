using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetCollectionPosts;

internal sealed class GetCollectionPostsQueryHandler
    : IRequestHandler<GetCollectionPostsQuery, (IReadOnlyList<PostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetCollectionPostsQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> Handle(
        GetCollectionPostsQuery request, CancellationToken cancellationToken)
        => _reader.GetCollectionPostsAsync(
            request.CollectionId, request.ViewerId, request.Page, request.PageSize, cancellationToken);
}

using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.SearchPosts;

internal sealed class SearchPostsQueryHandler
    : IRequestHandler<SearchPostsQuery, (IReadOnlyList<PostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public SearchPostsQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> Handle(
        SearchPostsQuery request, CancellationToken cancellationToken)
        => _reader.SearchPostsAsync(
            request.SearchTerm, request.UserId, request.Page, request.PageSize, cancellationToken);
}

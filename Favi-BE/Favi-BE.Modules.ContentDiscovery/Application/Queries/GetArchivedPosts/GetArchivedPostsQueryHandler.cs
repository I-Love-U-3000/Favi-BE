using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetArchivedPosts;

internal sealed class GetArchivedPostsQueryHandler
    : IRequestHandler<GetArchivedPostsQuery, (IReadOnlyList<PostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetArchivedPostsQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> Handle(
        GetArchivedPostsQuery request, CancellationToken cancellationToken)
        => _reader.GetArchivedPostsAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}

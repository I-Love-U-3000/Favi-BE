using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetProfilePosts;

internal sealed class GetProfilePostsQueryHandler
    : IRequestHandler<GetProfilePostsQuery, (IReadOnlyList<PostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetProfilePostsQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> Handle(
        GetProfilePostsQuery request, CancellationToken cancellationToken)
        => _reader.GetProfilePostsAsync(
            request.ProfileId, request.ViewerId, request.Page, request.PageSize, cancellationToken);
}

using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetFeedWithReposts;

internal sealed class GetFeedWithRepostsQueryHandler
    : IRequestHandler<GetFeedWithRepostsQuery, (IReadOnlyList<FeedItemReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetFeedWithRepostsQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<FeedItemReadModel> Items, int TotalCount)> Handle(
        GetFeedWithRepostsQuery request, CancellationToken cancellationToken)
        => _reader.GetFeedWithRepostsAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}

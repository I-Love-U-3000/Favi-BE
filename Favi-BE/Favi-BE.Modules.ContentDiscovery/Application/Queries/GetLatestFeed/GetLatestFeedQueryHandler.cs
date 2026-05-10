using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetLatestFeed;

internal sealed class GetLatestFeedQueryHandler
    : IRequestHandler<GetLatestFeedQuery, (IReadOnlyList<PostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetLatestFeedQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> Handle(
        GetLatestFeedQuery request, CancellationToken cancellationToken)
        => _reader.GetLatestFeedAsync(request.Page, request.PageSize, cancellationToken);
}

using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetNewsFeed;

internal sealed class GetNewsFeedQueryHandler
    : IRequestHandler<GetNewsFeedQuery, (IReadOnlyList<PostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetNewsFeedQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> Handle(
        GetNewsFeedQuery request, CancellationToken cancellationToken)
        => _reader.GetNewsFeedAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}

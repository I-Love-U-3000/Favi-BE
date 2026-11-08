using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetExperimentalFeed;

public sealed record GetExperimentalFeedQuery(Guid UserId, string Method, int Page, int PageSize)
    : IQuery<ExperimentalFeedResult?>;

internal sealed class GetExperimentalFeedQueryHandler(IExperimentalFeedQueryReader reader)
    : IRequestHandler<GetExperimentalFeedQuery, ExperimentalFeedResult?>
{
    public Task<ExperimentalFeedResult?> Handle(GetExperimentalFeedQuery request, CancellationToken ct)
        => reader.GetAsync(request.UserId, request.Method, request.Page, request.PageSize, ct);
}

using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRepostsByProfile;

internal sealed class GetRepostsByProfileQueryHandler
    : IRequestHandler<GetRepostsByProfileQuery, (IReadOnlyList<RepostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetRepostsByProfileQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<RepostReadModel> Items, int TotalCount)> Handle(
        GetRepostsByProfileQuery request, CancellationToken cancellationToken)
        => _reader.GetRepostsByProfileAsync(
            request.ProfileId, request.ViewerId, request.Page, request.PageSize, cancellationToken);
}

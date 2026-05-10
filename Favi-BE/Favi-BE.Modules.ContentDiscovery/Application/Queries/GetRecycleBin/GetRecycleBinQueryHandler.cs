using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRecycleBin;

internal sealed class GetRecycleBinQueryHandler
    : IRequestHandler<GetRecycleBinQuery, (IReadOnlyList<PostReadModel> Items, int TotalCount)>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetRecycleBinQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> Handle(
        GetRecycleBinQuery request, CancellationToken cancellationToken)
        => _reader.GetRecycleBinAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}

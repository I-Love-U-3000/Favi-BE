using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRepostById;

internal sealed class GetRepostByIdQueryHandler : IRequestHandler<GetRepostByIdQuery, RepostReadModel?>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetRepostByIdQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<RepostReadModel?> Handle(GetRepostByIdQuery request, CancellationToken cancellationToken)
        => _reader.GetRepostByIdAsync(request.RepostId, request.ViewerId, cancellationToken);
}

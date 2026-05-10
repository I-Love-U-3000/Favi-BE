using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetPostById;

internal sealed class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, PostReadModel?>
{
    private readonly IContentDiscoveryQueryReader _reader;

    public GetPostByIdQueryHandler(IContentDiscoveryQueryReader reader) => _reader = reader;

    public Task<PostReadModel?> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
        => _reader.GetPostByIdAsync(request.PostId, request.ViewerId, cancellationToken);
}

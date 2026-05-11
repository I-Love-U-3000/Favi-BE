using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetProfilePoster;

internal sealed class GetProfilePosterQueryHandler : IRequestHandler<GetProfilePosterQuery, string?>
{
    private readonly IAuthQueryReader _reader;

    public GetProfilePosterQueryHandler(IAuthQueryReader reader) => _reader = reader;

    public Task<string?> Handle(GetProfilePosterQuery request, CancellationToken cancellationToken)
        => _reader.GetPosterUrlAsync(request.ProfileId, cancellationToken);
}

using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetProfileAvatar;

internal sealed class GetProfileAvatarQueryHandler : IRequestHandler<GetProfileAvatarQuery, string?>
{
    private readonly IAuthQueryReader _reader;

    public GetProfileAvatarQueryHandler(IAuthQueryReader reader) => _reader = reader;

    public Task<string?> Handle(GetProfileAvatarQuery request, CancellationToken cancellationToken)
        => _reader.GetAvatarUrlAsync(request.ProfileId, cancellationToken);
}

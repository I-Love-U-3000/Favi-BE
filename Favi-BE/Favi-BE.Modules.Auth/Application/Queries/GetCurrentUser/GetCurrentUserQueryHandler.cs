using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto?>
{
    private readonly IAuthQueryReader _reader;

    public GetCurrentUserQueryHandler(IAuthQueryReader reader)
    {
        _reader = reader;
    }

    public Task<CurrentUserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        => _reader.GetCurrentUserAsync(request.ProfileId, cancellationToken);
}

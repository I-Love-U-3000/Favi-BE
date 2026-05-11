using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetProfileById;

internal sealed class GetProfileByIdQueryHandler : IRequestHandler<GetProfileByIdQuery, ProfileReadModel?>
{
    private readonly IAuthQueryReader _reader;

    public GetProfileByIdQueryHandler(IAuthQueryReader reader) => _reader = reader;

    public Task<ProfileReadModel?> Handle(GetProfileByIdQuery request, CancellationToken cancellationToken)
        => _reader.GetProfileByIdAsync(request.ProfileId, request.ViewerId, cancellationToken);
}

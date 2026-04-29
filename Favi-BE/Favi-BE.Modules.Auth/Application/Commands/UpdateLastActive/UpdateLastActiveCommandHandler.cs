using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;

internal sealed class UpdateLastActiveCommandHandler : IRequestHandler<UpdateLastActiveCommand>
{
    private readonly IAuthWriteRepository _repo;

    public UpdateLastActiveCommandHandler(IAuthWriteRepository repo) => _repo = repo;

    public async Task Handle(UpdateLastActiveCommand request, CancellationToken cancellationToken)
    {
        await _repo.UpdateLastActiveAsync(request.ProfileId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);
    }
}

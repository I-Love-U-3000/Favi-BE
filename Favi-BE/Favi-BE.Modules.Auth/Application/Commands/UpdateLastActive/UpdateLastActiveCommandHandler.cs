using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;

internal sealed class UpdateLastActiveCommandHandler : IRequestHandler<UpdateLastActiveCommand, DateTime>
{
    private readonly IAuthWriteRepository _repo;

    public UpdateLastActiveCommandHandler(IAuthWriteRepository repo) => _repo = repo;

    public async Task<DateTime> Handle(UpdateLastActiveCommand request, CancellationToken cancellationToken)
    {
        var ts = await _repo.UpdateLastActiveAsync(request.ProfileId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);
        return ts;
    }
}

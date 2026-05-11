using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.SyncProfile;

internal sealed class SyncProfileCommandHandler : IRequestHandler<SyncProfileCommand, bool>
{
    private readonly IAuthWriteRepository _repo;

    public SyncProfileCommandHandler(IAuthWriteRepository repo) => _repo = repo;

    public async Task<bool> Handle(SyncProfileCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.ProfileExistsAsync(request.UserId, cancellationToken))
            return false;

        await _repo.CreateProfileIfNotExistsAsync(request.UserId, request.Username, request.DisplayName, cancellationToken);
        await _repo.SaveAsync(cancellationToken);
        return true;
    }
}

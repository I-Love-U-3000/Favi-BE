using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.DeleteProfile;

internal sealed class DeleteProfileCommandHandler : IRequestHandler<DeleteProfileCommand, bool>
{
    private readonly IAuthWriteRepository _repo;

    public DeleteProfileCommandHandler(IAuthWriteRepository repo) => _repo = repo;

    public async Task<bool> Handle(DeleteProfileCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _repo.DeleteProfileAsync(request.ProfileId, cancellationToken);
        if (deleted)
            await _repo.SaveAsync(cancellationToken);
        return deleted;
    }
}

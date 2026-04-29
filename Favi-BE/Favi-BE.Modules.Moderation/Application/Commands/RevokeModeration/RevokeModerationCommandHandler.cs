using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;
using Favi_BE.Modules.Moderation.Application.Responses;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.RevokeModeration;

internal sealed class RevokeModerationCommandHandler : IRequestHandler<RevokeModerationCommand, ModerationCommandResult>
{
    private readonly IModerationCommandRepository _repo;

    public RevokeModerationCommandHandler(IModerationCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<ModerationCommandResult> Handle(RevokeModerationCommand request, CancellationToken cancellationToken)
    {
        var active = await _repo.GetActiveBanAsync(request.ProfileId, cancellationToken);
        if (active is null)
            return ModerationCommandResult.Fail("ACTIVE_BAN_NOT_FOUND");

        var now = DateTime.UtcNow;

        await _repo.RevokeUserModerationAsync(active.Id, now, cancellationToken);

        var adminAction = new AdminActionWriteData
        {
            Id = Guid.NewGuid(),
            AdminId = request.AdminId,
            ActionType = AdminActionType.UnbanUser,
            TargetProfileId = request.ProfileId,
            Notes = request.Reason,
            CreatedAt = now
        };

        await _repo.AddAdminActionAsync(adminAction, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return ModerationCommandResult.Success();
    }
}

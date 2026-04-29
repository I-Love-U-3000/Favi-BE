using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.ModerateUser;

internal sealed class ModerateUserCommandHandler : IRequestHandler<ModerateUserCommand, UserModerationReadModel?>
{
    private readonly IModerationCommandRepository _repo;

    public ModerateUserCommandHandler(IModerationCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<UserModerationReadModel?> Handle(ModerateUserCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.ProfileExistsAsync(request.ProfileId, cancellationToken))
            return null;

        var now = DateTime.UtcNow;
        var adminActionId = Guid.NewGuid();
        var moderationId = Guid.NewGuid();

        var adminActionType = request.ActionType == ModerationActionType.Ban
            ? AdminActionType.BanUser
            : AdminActionType.WarnUser;

        var adminAction = new AdminActionWriteData
        {
            Id = adminActionId,
            AdminId = request.AdminId,
            ActionType = adminActionType,
            TargetProfileId = request.ProfileId,
            Notes = request.Reason,
            CreatedAt = now
        };

        var moderation = new UserModerationWriteData
        {
            Id = moderationId,
            ProfileId = request.ProfileId,
            AdminId = request.AdminId,
            AdminActionId = adminActionId,
            ActionType = request.ActionType,
            Reason = request.Reason,
            CreatedAt = now,
            ExpiresAt = request.DurationDays.HasValue
                ? now.AddDays(request.DurationDays.Value)
                : null,
            Active = true
        };

        await _repo.AddAdminActionAsync(adminAction, cancellationToken);
        await _repo.AddUserModerationAsync(moderation, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return new UserModerationReadModel(
            moderationId,
            request.ProfileId,
            request.ActionType,
            request.Reason,
            now,
            moderation.ExpiresAt,
            null,
            true,
            adminActionId,
            request.AdminId);
    }
}

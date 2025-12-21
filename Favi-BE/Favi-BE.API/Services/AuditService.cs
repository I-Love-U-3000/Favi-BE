using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;

    public AuditService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AdminAction> LogAsync(AdminAction action, bool saveChanges = true)
    {
        if (action.Id == Guid.Empty)
            action.Id = Guid.NewGuid();

        if (action.CreatedAt == default)
            action.CreatedAt = DateTime.UtcNow;

        await _uow.AdminActions.AddAsync(action);
        if (saveChanges)
            await _uow.CompleteAsync();

        return action;
    }

    public Task<AdminAction> LogUserActionAsync(
        Guid adminId,
        AdminActionType actionType,
        Guid? targetProfileId,
        string? notes = null,
        Guid? reportId = null,
        bool saveChanges = true)
    {
        var action = new AdminAction
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            ActionType = actionType,
            TargetProfileId = targetProfileId,
            Notes = notes,
            ReportId = reportId,
            CreatedAt = DateTime.UtcNow
        };

        return LogAsync(action, saveChanges);
    }
}

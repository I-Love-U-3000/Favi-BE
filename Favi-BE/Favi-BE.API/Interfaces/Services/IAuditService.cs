using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Services
{
    public interface IAuditService
    {
        Task<AdminAction> LogAsync(AdminAction action, bool saveChanges = true);
        Task<AdminAction> LogUserActionAsync(
            Guid adminId,
            AdminActionType actionType,
            Guid? targetProfileId,
            string? notes = null,
            Guid? reportId = null,
            bool saveChanges = true);
    }
}

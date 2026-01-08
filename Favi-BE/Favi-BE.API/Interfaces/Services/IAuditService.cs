using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Services;

public interface IAuditService
{
    // Existing methods
    Task<AdminAction> LogAsync(AdminAction action, bool saveChanges = true);
    
    Task<AdminAction> LogUserActionAsync(
        Guid adminId,
        AdminActionType actionType,
        Guid? targetProfileId,
        string? notes = null,
        Guid? reportId = null,
        bool saveChanges = true);

    // New methods for Admin Portal
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterRequest filter);
    Task<IEnumerable<ActionTypeInfo>> GetActionTypesAsync();
    Task<IEnumerable<AuditActionTypeSummary>> GetActionTypeSummaryAsync(DateTime? fromDate, DateTime? toDate);
    Task<AuditLogDto?> GetLogByIdAsync(Guid id);

    // Export logging
    Task LogExportAsync(Guid adminId, string dataType, int recordCount, string format, bool saveChanges = true);
}

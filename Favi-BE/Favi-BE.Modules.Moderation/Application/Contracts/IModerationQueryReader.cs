using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts;

public interface IModerationQueryReader
{
    Task<(IReadOnlyList<ReportReadModel> Items, int Total)> GetReportsAsync(
        int page, int pageSize, ReportStatus? status, ReportTarget? targetType, CancellationToken ct = default);

    Task<ReportReadModel?> GetReportByIdAsync(Guid reportId, CancellationToken ct = default);

    Task<(IReadOnlyList<UserModerationReadModel> Items, int Total)> GetUserModerationHistoryAsync(
        Guid profileId, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<AdminActionReadModel> Items, int Total)> GetAdminActionAuditAsync(
        int page, int pageSize,
        AdminActionType? actionType, Guid? adminId, Guid? targetProfileId,
        DateTime? fromDate, DateTime? toDate, string? search,
        CancellationToken ct = default);

    Task<AdminActionReadModel?> GetAdminActionByIdAsync(Guid actionId, CancellationToken ct = default);
}

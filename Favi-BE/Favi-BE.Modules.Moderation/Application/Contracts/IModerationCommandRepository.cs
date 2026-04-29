using Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;
using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts;

public interface IModerationCommandRepository
{
    Task AddReportAsync(ReportWriteData data, CancellationToken ct = default);
    Task<ReportWriteData?> GetReportAsync(Guid reportId, CancellationToken ct = default);
    Task UpdateReportStatusAsync(Guid reportId, ReportStatus status, DateTime actedAt, CancellationToken ct = default);
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);
    Task<UserModerationWriteData?> GetActiveBanAsync(Guid profileId, CancellationToken ct = default);
    Task AddUserModerationAsync(UserModerationWriteData data, CancellationToken ct = default);
    Task RevokeUserModerationAsync(Guid moderationId, DateTime revokedAt, CancellationToken ct = default);
    Task AddAdminActionAsync(AdminActionWriteData data, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

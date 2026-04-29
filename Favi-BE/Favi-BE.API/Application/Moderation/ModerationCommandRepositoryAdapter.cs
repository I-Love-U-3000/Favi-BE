using Favi_BE.Interfaces;
using Favi_BE.Models.Entities;
using LegacyEntities = Favi_BE.Models.Entities;
using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;
using Favi_BE.Modules.Moderation.Domain;
using LegacyAdminActionType = Favi_BE.Models.Enums.AdminActionType;
using LegacyModerationActionType = Favi_BE.Models.Enums.ModerationActionType;
using LegacyReportStatus = Favi_BE.Models.Enums.ReportStatus;
using LegacyReportTarget = Favi_BE.Models.Enums.ReportTarget;

namespace Favi_BE.API.Application.Moderation;

internal sealed class ModerationCommandRepositoryAdapter : IModerationCommandRepository
{
    private readonly IUnitOfWork _uow;

    public ModerationCommandRepositoryAdapter(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task AddReportAsync(ReportWriteData data, CancellationToken ct = default)
        => await _uow.Reports.AddAsync(new Report
        {
            Id = data.Id,
            ReporterId = data.ReporterId,
            TargetType = (LegacyReportTarget)(int)data.TargetType,
            TargetId = data.TargetId,
            Reason = data.Reason,
            Status = (LegacyReportStatus)(int)data.Status,
            CreatedAt = data.CreatedAt,
            Data = data.Data
        });

    public async Task<ReportWriteData?> GetReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report is null) return null;

        return new ReportWriteData
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            TargetType = (ReportTarget)(int)report.TargetType,
            TargetId = report.TargetId,
            Reason = report.Reason,
            Status = (ReportStatus)(int)report.Status,
            CreatedAt = report.CreatedAt,
            ActedAt = report.ActedAt,
            Data = report.Data
        };
    }

    public async Task UpdateReportStatusAsync(Guid reportId, ReportStatus status, DateTime actedAt, CancellationToken ct = default)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report is null) return;

        report.Status = (LegacyReportStatus)(int)status;
        report.ActedAt = actedAt;
        _uow.Reports.Update(report);
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    public async Task<UserModerationWriteData?> GetActiveBanAsync(Guid profileId, CancellationToken ct = default)
    {
        var m = await _uow.UserModerations.GetActiveModerationAsync(profileId, LegacyModerationActionType.Ban);
        if (m is null) return null;

        return new UserModerationWriteData
        {
            Id = m.Id,
            ProfileId = m.ProfileId,
            AdminId = m.AdminId,
            AdminActionId = m.AdminActionId,
            ActionType = (ModerationActionType)(int)m.ActionType,
            Reason = m.Reason,
            CreatedAt = m.CreatedAt,
            ExpiresAt = m.ExpiresAt,
            RevokedAt = m.RevokedAt,
            Active = m.Active
        };
    }

    public async Task AddUserModerationAsync(UserModerationWriteData data, CancellationToken ct = default)
        => await _uow.UserModerations.AddAsync(new UserModeration
        {
            Id = data.Id,
            ProfileId = data.ProfileId,
            AdminId = data.AdminId,
            AdminActionId = data.AdminActionId,
            ActionType = (LegacyModerationActionType)(int)data.ActionType,
            Reason = data.Reason,
            CreatedAt = data.CreatedAt,
            ExpiresAt = data.ExpiresAt,
            Active = data.Active
        });

    public async Task RevokeUserModerationAsync(Guid moderationId, DateTime revokedAt, CancellationToken ct = default)
    {
        var m = await _uow.UserModerations.GetByIdAsync(moderationId);
        if (m is null) return;

        m.Active = false;
        m.RevokedAt = revokedAt;
        _uow.UserModerations.Update(m);
    }

    public async Task AddAdminActionAsync(AdminActionWriteData data, CancellationToken ct = default)
        => await _uow.AdminActions.AddAsync(new AdminAction
        {
            Id = data.Id,
            AdminId = data.AdminId,
            ActionType = (LegacyAdminActionType)(int)data.ActionType,
            TargetProfileId = data.TargetProfileId,
            TargetEntityId = data.TargetEntityId,
            TargetEntityType = data.TargetEntityType,
            ReportId = data.ReportId,
            Notes = data.Notes,
            CreatedAt = data.CreatedAt
        });

    public Task SaveAsync(CancellationToken ct = default)
        => _uow.CompleteAsync();
}

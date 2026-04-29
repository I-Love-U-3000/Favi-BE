using Favi_BE.Data;
using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Domain;
using Microsoft.EntityFrameworkCore;
using LegacyAdminActionType = Favi_BE.Models.Enums.AdminActionType;
using LegacyModerationActionType = Favi_BE.Models.Enums.ModerationActionType;
using LegacyReportStatus = Favi_BE.Models.Enums.ReportStatus;
using LegacyReportTarget = Favi_BE.Models.Enums.ReportTarget;

namespace Favi_BE.API.Application.Moderation;

internal sealed class ModerationQueryReaderAdapter : IModerationQueryReader
{
    private readonly AppDbContext _db;

    public ModerationQueryReaderAdapter(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<ReportReadModel> Items, int Total)> GetReportsAsync(
        int page, int pageSize, ReportStatus? status, ReportTarget? targetType, CancellationToken ct = default)
    {
        var q = _db.Reports.AsNoTracking();

        if (status.HasValue)
            q = q.Where(r => r.Status == (LegacyReportStatus)(int)status.Value);

        if (targetType.HasValue)
            q = q.Where(r => r.TargetType == (LegacyReportTarget)(int)targetType.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportReadModel(
                r.Id, r.ReporterId,
                (ReportTarget)(int)r.TargetType, r.TargetId,
                r.Reason, (ReportStatus)(int)r.Status,
                r.CreatedAt, r.ActedAt, r.Data))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<ReportReadModel?> GetReportByIdAsync(Guid reportId, CancellationToken ct = default)
    {
        var r = await _db.Reports.AsNoTracking()
            .Where(x => x.Id == reportId)
            .Select(x => new ReportReadModel(
                x.Id, x.ReporterId,
                (ReportTarget)(int)x.TargetType, x.TargetId,
                x.Reason, (ReportStatus)(int)x.Status,
                x.CreatedAt, x.ActedAt, x.Data))
            .FirstOrDefaultAsync(ct);

        return r;
    }

    public async Task<(IReadOnlyList<UserModerationReadModel> Items, int Total)> GetUserModerationHistoryAsync(
        Guid profileId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.UserModerations.AsNoTracking()
            .Where(m => m.ProfileId == profileId);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new UserModerationReadModel(
                m.Id, m.ProfileId,
                (ModerationActionType)(int)m.ActionType,
                m.Reason, m.CreatedAt, m.ExpiresAt, m.RevokedAt,
                m.Active, m.AdminActionId, m.AdminId))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<AdminActionReadModel> Items, int Total)> GetAdminActionAuditAsync(
        int page, int pageSize,
        AdminActionType? actionType, Guid? adminId, Guid? targetProfileId,
        DateTime? fromDate, DateTime? toDate, string? search,
        CancellationToken ct = default)
    {
        var q = _db.AdminActions.AsNoTracking()
            .Include(a => a.Admin)
            .AsQueryable();

        if (actionType.HasValue)
            q = q.Where(a => a.ActionType == (LegacyAdminActionType)(int)actionType.Value);

        if (adminId.HasValue)
            q = q.Where(a => a.AdminId == adminId.Value);

        if (targetProfileId.HasValue)
            q = q.Where(a => a.TargetProfileId == targetProfileId.Value);

        if (fromDate.HasValue)
            q = q.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            q = q.Where(a => a.CreatedAt <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(a => a.Notes != null && a.Notes.Contains(search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminActionReadModel(
                a.Id, a.AdminId,
                a.Admin != null ? a.Admin.Username : null,
                a.Admin != null ? a.Admin.DisplayName : null,
                (AdminActionType)(int)a.ActionType,
                a.TargetProfileId, null, null,
                a.TargetEntityId, a.TargetEntityType,
                a.ReportId, a.Notes, a.CreatedAt))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<AdminActionReadModel?> GetAdminActionByIdAsync(Guid actionId, CancellationToken ct = default)
    {
        var a = await _db.AdminActions.AsNoTracking()
            .Include(x => x.Admin)
            .Where(x => x.Id == actionId)
            .Select(x => new AdminActionReadModel(
                x.Id, x.AdminId,
                x.Admin != null ? x.Admin.Username : null,
                x.Admin != null ? x.Admin.DisplayName : null,
                (AdminActionType)(int)x.ActionType,
                x.TargetProfileId, null, null,
                x.TargetEntityId, x.TargetEntityType,
                x.ReportId, x.Notes, x.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return a;
    }
}

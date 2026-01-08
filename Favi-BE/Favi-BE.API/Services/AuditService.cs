using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AuditService> _logger;

    // Display names for action types
    private static readonly Dictionary<AdminActionType, string> ActionTypeDisplayNames = new()
    {
        { AdminActionType.Unknown, "Unknown" },
        { AdminActionType.BanUser, "Ban User" },
        { AdminActionType.UnbanUser, "Unban User" },
        { AdminActionType.WarnUser, "Warn User" },
        { AdminActionType.ResolveReport, "Resolve Report" },
        { AdminActionType.DeleteContent, "Delete Content" },
        { AdminActionType.ExportData, "Export Data" }
    };

    public AuditService(IUnitOfWork uow, ILogger<AuditService> logger)
    {
        _uow = uow;
        _logger = logger;
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

    public async Task LogExportAsync(Guid adminId, string dataType, int recordCount, string format, bool saveChanges = true)
    {
        var action = new AdminAction
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            ActionType = AdminActionType.ExportData,
            TargetEntityType = dataType,
            Notes = $"Exported {recordCount} {dataType} records in {format.ToUpper()} format",
            CreatedAt = DateTime.UtcNow
        };

        await LogAsync(action, saveChanges);
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterRequest filter)
    {
        try
        {
            var allActions = await _uow.AdminActions.GetAllAsync();
            var queryable = allActions.AsQueryable();

            // Filter by action type
            if (filter.ActionType.HasValue)
            {
                queryable = queryable.Where(a => a.ActionType == filter.ActionType.Value);
            }

            // Filter by admin ID
            if (filter.AdminId.HasValue)
            {
                queryable = queryable.Where(a => a.AdminId == filter.AdminId.Value);
            }

            // Filter by target profile ID
            if (filter.TargetProfileId.HasValue)
            {
                queryable = queryable.Where(a => a.TargetProfileId == filter.TargetProfileId.Value);
            }

            // Filter by date range
            if (filter.FromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Utc);
                queryable = queryable.Where(a => a.CreatedAt >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                queryable = queryable.Where(a => a.CreatedAt < toUtc);
            }

            // Search in notes
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                queryable = queryable.Where(a => 
                    a.Notes != null && a.Notes.ToLower().Contains(searchLower));
            }

            // Order by created at descending (newest first)
            queryable = queryable.OrderByDescending(a => a.CreatedAt);

            var total = queryable.Count();
            var skip = (filter.Page - 1) * filter.PageSize;
            var items = queryable.Skip(skip).Take(filter.PageSize).ToList();

            // Get all profiles for admin and target display names
            var allProfiles = await _uow.Profiles.GetAllAsync();
            var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);

            var dtos = items.Select(a =>
            {
                profileDict.TryGetValue(a.AdminId, out var adminProfile);
                Profile? targetProfile = null;
                if (a.TargetProfileId.HasValue)
                    profileDict.TryGetValue(a.TargetProfileId.Value, out targetProfile);

                return new AuditLogDto(
                    a.Id,
                    a.AdminId,
                    adminProfile?.Username,
                    adminProfile?.DisplayName,
                    a.ActionType,
                    GetActionTypeDisplayName(a.ActionType),
                    a.TargetProfileId,
                    targetProfile?.Username,
                    targetProfile?.DisplayName,
                    a.TargetEntityId,
                    a.TargetEntityType,
                    a.ReportId,
                    a.Notes,
                    a.CreatedAt
                );
            });

            return new PagedResult<AuditLogDto>(dtos, filter.Page, filter.PageSize, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            throw;
        }
    }

    public Task<IEnumerable<ActionTypeInfo>> GetActionTypesAsync()
    {
        var actionTypes = Enum.GetValues<AdminActionType>()
            .Where(t => t != AdminActionType.Unknown)
            .Select(t => new ActionTypeInfo(
                t,
                t.ToString(),
                GetActionTypeDisplayName(t)
            ));

        return Task.FromResult(actionTypes);
    }

    public async Task<IEnumerable<AuditActionTypeSummary>> GetActionTypeSummaryAsync(
        DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            var allActions = await _uow.AdminActions.GetAllAsync();
            var queryable = allActions.AsQueryable();

            if (fromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
                queryable = queryable.Where(a => a.CreatedAt >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                queryable = queryable.Where(a => a.CreatedAt < toUtc);
            }

            var summary = queryable
                .GroupBy(a => a.ActionType)
                .Select(g => new AuditActionTypeSummary(
                    g.Key,
                    GetActionTypeDisplayName(g.Key),
                    g.Count()
                ))
                .OrderByDescending(s => s.Count)
                .ToList();

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit action type summary");
            throw;
        }
    }

    public async Task<AuditLogDto?> GetLogByIdAsync(Guid id)
    {
        try
        {
            var action = await _uow.AdminActions.GetByIdAsync(id);
            if (action == null)
                return null;

            var allProfiles = await _uow.Profiles.GetAllAsync();
            var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);

            profileDict.TryGetValue(action.AdminId, out var adminProfile);
            Profile? targetProfile = null;
            if (action.TargetProfileId.HasValue)
                profileDict.TryGetValue(action.TargetProfileId.Value, out targetProfile);

            return new AuditLogDto(
                action.Id,
                action.AdminId,
                adminProfile?.Username,
                adminProfile?.DisplayName,
                action.ActionType,
                GetActionTypeDisplayName(action.ActionType),
                action.TargetProfileId,
                targetProfile?.Username,
                targetProfile?.DisplayName,
                action.TargetEntityId,
                action.TargetEntityType,
                action.ReportId,
                action.Notes,
                action.CreatedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log by id {Id}", id);
            throw;
        }
    }

    private static string GetActionTypeDisplayName(AdminActionType actionType)
    {
        return ActionTypeDisplayNames.TryGetValue(actionType, out var displayName)
            ? displayName
            : actionType.ToString();
    }
}

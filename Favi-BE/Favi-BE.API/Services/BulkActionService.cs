using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Services;

public class BulkActionService : IBulkActionService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly ILogger<BulkActionService> _logger;

    private const int MaxBulkItems = 100;

    public BulkActionService(
        IUnitOfWork uow,
        IAuditService auditService,
        ILogger<BulkActionService> logger)
    {
        _uow = uow;
        _auditService = auditService;
        _logger = logger;
    }

    // ============================================================
    // USER MODERATION
    // ============================================================

    public async Task<BulkActionResponse> BulkBanAsync(
        IEnumerable<Guid> profileIds,
        Guid adminId,
        string reason,
        int? durationDays)
    {
        var ids = profileIds.Distinct().Take(MaxBulkItems).ToList();
        var results = new List<BulkActionItemResult>();

        foreach (var profileId in ids)
        {
            try
            {
                var profile = await _uow.Profiles.GetByIdAsync(profileId);
                if (profile is null)
                {
                    results.Add(new BulkActionItemResult(profileId, false, "Profile not found"));
                    continue;
                }

                if (profile.IsBanned)
                {
                    results.Add(new BulkActionItemResult(profileId, false, "User is already banned"));
                    continue;
                }

                // Deactivate existing ban if any
                var existingBan = await _uow.UserModerations.GetActiveModerationAsync(profileId, ModerationActionType.Ban);
                if (existingBan is not null)
                {
                    existingBan.Active = false;
                    existingBan.RevokedAt = DateTime.UtcNow;
                    _uow.UserModerations.Update(existingBan);
                }

                var expiresAt = durationDays.HasValue
                    ? DateTime.UtcNow.AddDays(durationDays.Value)
                    : (DateTime?)null;

                profile.IsBanned = true;
                profile.BannedUntil = expiresAt;

                var adminAction = await _auditService.LogUserActionAsync(
                    adminId,
                    AdminActionType.BanUser,
                    profile.Id,
                    $"[Bulk] {reason}",
                    saveChanges: false);

                var moderation = new UserModeration
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profile.Id,
                    AdminId = adminId,
                    AdminActionId = adminAction.Id,
                    ActionType = ModerationActionType.Ban,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    Active = true
                };

                await _uow.UserModerations.AddAsync(moderation);
                results.Add(new BulkActionItemResult(profileId, true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning profile {ProfileId}", profileId);
                results.Add(new BulkActionItemResult(profileId, false, ex.Message));
            }
        }

        await _uow.CompleteAsync();

        return CreateResponse(ids.Count, results);
    }

    public async Task<BulkActionResponse> BulkUnbanAsync(
        IEnumerable<Guid> profileIds,
        Guid adminId,
        string? reason)
    {
        var ids = profileIds.Distinct().Take(MaxBulkItems).ToList();
        var results = new List<BulkActionItemResult>();

        foreach (var profileId in ids)
        {
            try
            {
                var profile = await _uow.Profiles.GetByIdAsync(profileId);
                if (profile is null)
                {
                    results.Add(new BulkActionItemResult(profileId, false, "Profile not found"));
                    continue;
                }

                if (!profile.IsBanned)
                {
                    results.Add(new BulkActionItemResult(profileId, false, "User is not banned"));
                    continue;
                }

                profile.IsBanned = false;
                profile.BannedUntil = null;

                var activeBan = await _uow.UserModerations.GetActiveModerationAsync(profileId, ModerationActionType.Ban);
                if (activeBan is not null)
                {
                    activeBan.Active = false;
                    activeBan.RevokedAt = DateTime.UtcNow;
                    _uow.UserModerations.Update(activeBan);
                }

                await _auditService.LogUserActionAsync(
                    adminId,
                    AdminActionType.UnbanUser,
                    profile.Id,
                    $"[Bulk] {reason ?? "Bulk unban"}",
                    saveChanges: false);

                results.Add(new BulkActionItemResult(profileId, true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning profile {ProfileId}", profileId);
                results.Add(new BulkActionItemResult(profileId, false, ex.Message));
            }
        }

        await _uow.CompleteAsync();

        return CreateResponse(ids.Count, results);
    }

    public async Task<BulkActionResponse> BulkWarnAsync(
        IEnumerable<Guid> profileIds,
        Guid adminId,
        string reason)
    {
        var ids = profileIds.Distinct().Take(MaxBulkItems).ToList();
        var results = new List<BulkActionItemResult>();

        foreach (var profileId in ids)
        {
            try
            {
                var profile = await _uow.Profiles.GetByIdAsync(profileId);
                if (profile is null)
                {
                    results.Add(new BulkActionItemResult(profileId, false, "Profile not found"));
                    continue;
                }

                var adminAction = await _auditService.LogUserActionAsync(
                    adminId,
                    AdminActionType.WarnUser,
                    profile.Id,
                    $"[Bulk] {reason}",
                    saveChanges: false);

                var moderation = new UserModeration
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profile.Id,
                    AdminId = adminId,
                    AdminActionId = adminAction.Id,
                    ActionType = ModerationActionType.Warn,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow,
                    Active = true
                };

                await _uow.UserModerations.AddAsync(moderation);
                results.Add(new BulkActionItemResult(profileId, true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warning profile {ProfileId}", profileId);
                results.Add(new BulkActionItemResult(profileId, false, ex.Message));
            }
        }

        await _uow.CompleteAsync();

        return CreateResponse(ids.Count, results);
    }

    // ============================================================
    // CONTENT MODERATION
    // ============================================================

    public async Task<BulkActionResponse> BulkDeletePostsAsync(
        IEnumerable<Guid> postIds,
        Guid adminId,
        string reason)
    {
        var ids = postIds.Distinct().Take(MaxBulkItems).ToList();
        var results = new List<BulkActionItemResult>();

        foreach (var postId in ids)
        {
            try
            {
                var post = await _uow.Posts.GetByIdAsync(postId);
                if (post is null)
                {
                    results.Add(new BulkActionItemResult(postId, false, "Post not found"));
                    continue;
                }

                if (post.DeletedDayExpiredAt.HasValue)
                {
                    results.Add(new BulkActionItemResult(postId, false, "Post is already deleted"));
                    continue;
                }

                // Soft delete
                post.DeletedDayExpiredAt = DateTime.UtcNow.AddDays(30);
                post.UpdatedAt = DateTime.UtcNow;
                _uow.Posts.Update(post);

                await _auditService.LogAsync(new AdminAction
                {
                    Id = Guid.NewGuid(),
                    AdminId = adminId,
                    ActionType = AdminActionType.DeleteContent,
                    TargetEntityId = postId,
                    TargetEntityType = "Post",
                    TargetProfileId = post.ProfileId,
                    Notes = $"[Bulk] {reason}",
                    CreatedAt = DateTime.UtcNow
                }, saveChanges: false);

                results.Add(new BulkActionItemResult(postId, true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                results.Add(new BulkActionItemResult(postId, false, ex.Message));
            }
        }

        await _uow.CompleteAsync();

        return CreateResponse(ids.Count, results);
    }

    public async Task<BulkActionResponse> BulkDeleteCommentsAsync(
        IEnumerable<Guid> commentIds,
        Guid adminId,
        string reason)
    {
        var ids = commentIds.Distinct().Take(MaxBulkItems).ToList();
        var results = new List<BulkActionItemResult>();

        foreach (var commentId in ids)
        {
            try
            {
                var comment = await _uow.Comments.GetByIdAsync(commentId);
                if (comment is null)
                {
                    results.Add(new BulkActionItemResult(commentId, false, "Comment not found"));
                    continue;
                }

                var targetProfileId = comment.ProfileId;

                _uow.Comments.Remove(comment);

                await _auditService.LogAsync(new AdminAction
                {
                    Id = Guid.NewGuid(),
                    AdminId = adminId,
                    ActionType = AdminActionType.DeleteContent,
                    TargetEntityId = commentId,
                    TargetEntityType = "Comment",
                    TargetProfileId = targetProfileId,
                    Notes = $"[Bulk] {reason}",
                    CreatedAt = DateTime.UtcNow
                }, saveChanges: false);

                results.Add(new BulkActionItemResult(commentId, true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                results.Add(new BulkActionItemResult(commentId, false, ex.Message));
            }
        }

        await _uow.CompleteAsync();

        return CreateResponse(ids.Count, results);
    }

    // ============================================================
    // REPORT MANAGEMENT
    // ============================================================

    public async Task<BulkActionResponse> BulkResolveReportsAsync(
        IEnumerable<Guid> reportIds,
        Guid adminId,
        ReportStatus newStatus)
    {
        var ids = reportIds.Distinct().Take(MaxBulkItems).ToList();
        var results = new List<BulkActionItemResult>();

        foreach (var reportId in ids)
        {
            try
            {
                var report = await _uow.Reports.GetByIdAsync(reportId);
                if (report is null)
                {
                    results.Add(new BulkActionItemResult(reportId, false, "Report not found"));
                    continue;
                }

                if (report.Status != ReportStatus.Pending)
                {
                    results.Add(new BulkActionItemResult(reportId, false, $"Report is already {report.Status}"));
                    continue;
                }

                report.Status = newStatus;
                report.ActedAt = DateTime.UtcNow;
                _uow.Reports.Update(report);

                if (newStatus == ReportStatus.Resolved)
                {
                    await _auditService.LogUserActionAsync(
                        adminId,
                        AdminActionType.ResolveReport,
                        null,
                        $"[Bulk] Resolved report {reportId}",
                        reportId,
                        saveChanges: false);
                }

                results.Add(new BulkActionItemResult(reportId, true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving report {ReportId}", reportId);
                results.Add(new BulkActionItemResult(reportId, false, ex.Message));
            }
        }

        await _uow.CompleteAsync();

        return CreateResponse(ids.Count, results);
    }

    // ============================================================
    // HELPER
    // ============================================================

    private static BulkActionResponse CreateResponse(int totalRequested, List<BulkActionItemResult> results)
    {
        var successCount = results.Count(r => r.Success);
        var failedCount = results.Count(r => !r.Success);

        return new BulkActionResponse(
            totalRequested,
            successCount,
            failedCount,
            results
        );
    }
}

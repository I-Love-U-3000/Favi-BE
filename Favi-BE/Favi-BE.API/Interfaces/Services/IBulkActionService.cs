using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Services;

public interface IBulkActionService
{
    // User moderation
    Task<BulkActionResponse> BulkBanAsync(IEnumerable<Guid> profileIds, Guid adminId, string reason, int? durationDays);
    Task<BulkActionResponse> BulkUnbanAsync(IEnumerable<Guid> profileIds, Guid adminId, string? reason);
    Task<BulkActionResponse> BulkWarnAsync(IEnumerable<Guid> profileIds, Guid adminId, string reason);

    // Content moderation
    Task<BulkActionResponse> BulkDeletePostsAsync(IEnumerable<Guid> postIds, Guid adminId, string reason);
    Task<BulkActionResponse> BulkDeleteCommentsAsync(IEnumerable<Guid> commentIds, Guid adminId, string reason);

    // Report management
    Task<BulkActionResponse> BulkResolveReportsAsync(IEnumerable<Guid> reportIds, Guid adminId, ReportStatus newStatus);
}

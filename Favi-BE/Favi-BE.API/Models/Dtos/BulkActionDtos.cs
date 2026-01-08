using System.ComponentModel.DataAnnotations;
using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos;

/// <summary>
/// Request for bulk ban users
/// </summary>
public record BulkBanRequest(
    [Required] IEnumerable<Guid> ProfileIds,
    [Required] string Reason,
    int? DurationDays = null
);

/// <summary>
/// Request for bulk unban users
/// </summary>
public record BulkUnbanRequest(
    [Required] IEnumerable<Guid> ProfileIds,
    string? Reason = null
);

/// <summary>
/// Request for bulk warn users
/// </summary>
public record BulkWarnRequest(
    [Required] IEnumerable<Guid> ProfileIds,
    [Required] string Reason
);

/// <summary>
/// Request for bulk delete posts
/// </summary>
public record BulkDeletePostsRequest(
    [Required] IEnumerable<Guid> PostIds,
    [Required] string Reason
);

/// <summary>
/// Request for bulk delete comments
/// </summary>
public record BulkDeleteCommentsRequest(
    [Required] IEnumerable<Guid> CommentIds,
    [Required] string Reason
);

/// <summary>
/// Request for bulk resolve reports
/// </summary>
public record BulkResolveReportsRequest(
    [Required] IEnumerable<Guid> ReportIds,
    [Required] ReportStatus NewStatus
);

/// <summary>
/// Request for bulk reject reports (convenience DTO)
/// </summary>
public record BulkRejectReportsRequest(
    [Required] IEnumerable<Guid> ReportIds
);

/// <summary>
/// Result of a single item in bulk operation
/// </summary>
public record BulkActionItemResult(
    Guid Id,
    bool Success,
    string? Error = null
);

/// <summary>
/// Response for bulk operations
/// </summary>
public record BulkActionResponse(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    IEnumerable<BulkActionItemResult> Results
);

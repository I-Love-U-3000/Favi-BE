namespace Favi_BE.Models.Dtos;

/// <summary>
/// Supported export formats
/// </summary>
public enum ExportFormat
{
    Csv,
    Json,
    Excel
}

/// <summary>
/// Export request for users data
/// </summary>
public record ExportUsersRequest(
    string? Search = null,
    string? Role = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    ExportFormat Format = ExportFormat.Csv
);

/// <summary>
/// Export request for posts data
/// </summary>
public record ExportPostsRequest(
    string? Search = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    ExportFormat Format = ExportFormat.Csv
);

/// <summary>
/// Export request for reports data
/// </summary>
public record ExportReportsRequest(
    string? Status = null,
    string? TargetType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    ExportFormat Format = ExportFormat.Csv
);

/// <summary>
/// Export request for audit logs
/// </summary>
public record ExportAuditLogsRequest(
    string? ActionType = null,
    Guid? AdminId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    ExportFormat Format = ExportFormat.Csv
);

/// <summary>
/// User data for export (flat structure)
/// </summary>
public record ExportUserDto(
    Guid Id,
    string? Username,
    string? DisplayName,
    string? Email,
    string Role,
    bool IsBanned,
    DateTime? BannedUntil,
    DateTime CreatedAt,
    DateTime? LastActiveAt,
    int PostsCount,
    int FollowersCount,
    int FollowingCount
);

/// <summary>
/// Post data for export (flat structure)
/// </summary>
public record ExportPostDto(
    Guid Id,
    Guid AuthorId,
    string? AuthorUsername,
    string? Caption,
    string Privacy,
    DateTime CreatedAt,
    bool IsDeleted,
    int ReactionsCount,
    int CommentsCount,
    int MediaCount
);

/// <summary>
/// Report data for export (flat structure)
/// </summary>
public record ExportReportDto(
    Guid Id,
    Guid ReporterId,
    string? ReporterUsername,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? ActedAt
);

/// <summary>
/// Audit log data for export (flat structure)
/// </summary>
public record ExportAuditLogDto(
    Guid Id,
    Guid AdminId,
    string? AdminUsername,
    string ActionType,
    Guid? TargetProfileId,
    string? TargetUsername,
    string? TargetEntityType,
    Guid? TargetEntityId,
    string? Notes,
    DateTime CreatedAt
);

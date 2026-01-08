using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos;

/// <summary>
/// DTO for audit log entry
/// </summary>
public record AuditLogDto(
    Guid Id,
    Guid AdminId,
    string? AdminUsername,
    string? AdminDisplayName,
    AdminActionType ActionType,
    string ActionTypeDisplayName,
    Guid? TargetProfileId,
    string? TargetUsername,
    string? TargetDisplayName,
    Guid? TargetEntityId,
    string? TargetEntityType,
    Guid? ReportId,
    string? Notes,
    DateTime CreatedAt
);

/// <summary>
/// Filter request for audit logs
/// </summary>
public record AuditLogFilterRequest
{
    public AdminActionType? ActionType { get; init; }
    public Guid? AdminId { get; init; }
    public Guid? TargetProfileId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Summary of action types with counts
/// </summary>
public record AuditActionTypeSummary(
    AdminActionType ActionType,
    string DisplayName,
    int Count
);

/// <summary>
/// Response for action types list
/// </summary>
public record ActionTypeInfo(
    AdminActionType Value,
    string Name,
    string DisplayName
);

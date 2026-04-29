using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;

public sealed record ReportReadModel(
    Guid Id,
    Guid ReporterId,
    ReportTarget TargetType,
    Guid TargetId,
    string? Reason,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime? ActedAt,
    string? Data
);

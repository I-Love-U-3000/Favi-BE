using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record CreateReportRequest(
        Guid ReporterProfileId,
        ReportTarget TargetType,
        Guid TargetId,
        string Reason
    );

    public record ReportResponse(
        Guid Id,
        Guid ReporterProfileId,
        ReportTarget TargetType,
        Guid TargetId,
        string Reason,
        ReportStatus Status,
        DateTime CreatedAt,
        DateTime? ActedAt,
        string? Data
    );

    public record UpdateReportStatusRequest(
        ReportStatus NewStatus
    );
}

using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;

public sealed class ReportWriteData
{
    public Guid Id { get; init; }
    public Guid ReporterId { get; init; }
    public ReportTarget TargetType { get; init; }
    public Guid TargetId { get; init; }
    public string? Reason { get; init; }
    public ReportStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ActedAt { get; set; }
    public string? Data { get; init; }
}

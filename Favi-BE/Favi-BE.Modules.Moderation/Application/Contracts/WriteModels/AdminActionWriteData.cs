using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;

public sealed class AdminActionWriteData
{
    public Guid Id { get; init; }
    public Guid AdminId { get; init; }
    public AdminActionType ActionType { get; init; }
    public Guid? TargetProfileId { get; init; }
    public Guid? TargetEntityId { get; init; }
    public string? TargetEntityType { get; init; }
    public Guid? ReportId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

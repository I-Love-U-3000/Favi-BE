using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;

public sealed class UserModerationWriteData
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public Guid AdminId { get; init; }
    public Guid AdminActionId { get; init; }
    public ModerationActionType ActionType { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; set; }
    public bool Active { get; set; }
}

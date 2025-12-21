using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities;

public class UserModeration
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public Guid AdminId { get; set; }
    public Guid AdminActionId { get; set; }
    public ModerationActionType ActionType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool Active { get; set; } = true;

    public Profile Profile { get; set; } = null!;
    public Profile Admin { get; set; } = null!;
    public AdminAction AdminAction { get; set; } = null!;
}

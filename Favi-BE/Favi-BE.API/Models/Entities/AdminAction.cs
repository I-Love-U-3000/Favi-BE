using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities;

public class AdminAction
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public AdminActionType ActionType { get; set; }
    public Guid? TargetProfileId { get; set; }
    public Guid? TargetEntityId { get; set; }
    public string? TargetEntityType { get; set; }
    public Guid? ReportId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Profile Admin { get; set; } = null!;
    public ICollection<UserModeration> UserModerations { get; set; } = new List<UserModeration>();
}

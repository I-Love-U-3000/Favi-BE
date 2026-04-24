using Favi_BE.Modules.Notifications.Domain;

namespace Favi_BE.Modules.Notifications.Application.Contracts;

/// <summary>
/// Module-internal representation of a notification to be persisted.
/// Decoupled from the EF entity so the module does not depend on the API project.
/// </summary>
public sealed record NotificationRecord(
    Guid Id,
    NotificationType Type,
    Guid RecipientProfileId,
    Guid ActorProfileId,
    Guid? TargetPostId,
    Guid? TargetCommentId,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);

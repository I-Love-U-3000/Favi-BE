using Favi_BE.Modules.Notifications.Domain;

namespace Favi_BE.Modules.Notifications.Application.Contracts;

/// <summary>
/// Module-internal DTO for real-time push payload sent to the client via SignalR.
/// The adapter maps this to the API-layer NotificationDto before sending.
/// </summary>
public sealed record NotificationPushData(
    Guid Id,
    NotificationType Type,
    Guid ActorProfileId,
    string ActorUsername,
    string? ActorDisplayName,
    string? ActorAvatarUrl,
    Guid? TargetPostId,
    Guid? TargetCommentId,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);

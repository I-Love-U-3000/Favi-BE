using Favi_BE.Modules.Notifications.Domain;

namespace Favi_BE.Modules.Notifications.Application.Contracts.ReadModels;

public sealed record NotificationReadModel(
    Guid Id,
    NotificationType Type,
    Guid RecipientProfileId,
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

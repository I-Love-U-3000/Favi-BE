namespace Favi_BE.Modules.Notifications.Domain.Events;

/// <summary>
/// Integration event raised when a user follows another user.
/// </summary>
public sealed record UserFollowedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid FollowerId,
    Guid FolloweeId,
    string Message,
    string ActorUsername,
    string? ActorDisplayName,
    string? ActorAvatarUrl
);

namespace Favi_BE.Modules.Notifications.Domain.Events;

/// <summary>
/// Integration event raised when a user reacts to a post.
/// Only raised when the reaction is added (not removed).
/// </summary>
public sealed record PostReactionToggledIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ActorId,
    Guid PostId,
    Guid RecipientId,
    string Message,
    string ActorUsername,
    string? ActorDisplayName,
    string? ActorAvatarUrl
);

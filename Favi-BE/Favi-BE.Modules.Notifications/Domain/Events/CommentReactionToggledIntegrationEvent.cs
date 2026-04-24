namespace Favi_BE.Modules.Notifications.Domain.Events;

/// <summary>
/// Integration event raised when a user reacts to a comment.
/// Only raised when the reaction is added (not removed).
/// </summary>
public sealed record CommentReactionToggledIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ActorId,
    Guid CommentId,
    Guid? TargetPostId,
    Guid RecipientId,
    string Message,
    string ActorUsername,
    string? ActorDisplayName,
    string? ActorAvatarUrl
);

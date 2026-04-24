namespace Favi_BE.Modules.Notifications.Domain.Events;

/// <summary>
/// Integration event raised when a user comments on a post.
/// Carries all resolved data so the consumer does not require extra DB reads.
/// </summary>
public sealed record CommentCreatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid AuthorId,
    Guid PostId,
    Guid CommentId,
    Guid RecipientId,
    string Message,
    string ActorUsername,
    string? ActorDisplayName,
    string? ActorAvatarUrl
);

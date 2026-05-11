using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.Engagement.Domain.Events;

public sealed record CommentReactionAddedDomainEvent(
    Guid ActorId,
    Guid CommentId,
    DateTime OccurredOnUtc) : IDomainEvent;

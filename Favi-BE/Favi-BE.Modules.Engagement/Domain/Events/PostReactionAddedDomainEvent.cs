using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.Engagement.Domain.Events;

public sealed record PostReactionAddedDomainEvent(
    Guid ActorId,
    Guid PostId,
    DateTime OccurredOnUtc) : IDomainEvent;

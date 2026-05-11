using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.SocialGraph.Domain.Events;

public sealed record UserFollowedDomainEvent(
    Guid FollowerId,
    Guid FolloweeId,
    DateTime OccurredOnUtc) : IDomainEvent;

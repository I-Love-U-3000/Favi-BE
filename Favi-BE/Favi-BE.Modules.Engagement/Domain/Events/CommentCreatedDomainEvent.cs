using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.Engagement.Domain.Events;

public sealed record CommentCreatedDomainEvent(
    Guid AuthorId,
    Guid PostId,
    Guid CommentId,
    DateTime OccurredOnUtc) : IDomainEvent;

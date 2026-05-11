using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Outbox;
using Favi_BE.BuildingBlocks.Domain;
using Favi_BE.Interfaces;
using Favi_BE.Modules.Notifications.Domain.Events;
using Favi_BE.Modules.SocialGraph.Domain.Events;

namespace Favi_BE.API.Application.SocialGraph;

/// <summary>
/// Maps SocialGraph domain events to integration events written to the outbox.
/// Resolves actor profile data so downstream notification consumers receive a self-contained payload.
/// </summary>
internal sealed class SocialGraphDomainNotificationsMapper : IModuleDomainEventMapper
{
    private readonly IUnitOfWork _uow;

    public SocialGraphDomainNotificationsMapper(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public bool CanHandle(IDomainEvent domainEvent) => domainEvent is UserFollowedDomainEvent;

    public async Task<OutboxMessageData?> MapAsync(IDomainEvent domainEvent, string? correlationId, string? causationId)
    {
        if (domainEvent is not UserFollowedDomainEvent e) return null;

        var actor = await _uow.Profiles.GetByIdAsync(e.FollowerId);
        if (actor is null) return null;

        var integrationEvent = new UserFollowedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: e.OccurredOnUtc,
            FollowerId: e.FollowerId,
            FolloweeId: e.FolloweeId,
            Message: $"{actor.DisplayName ?? actor.Username} started following you",
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        return new OutboxMessageData(
            Id: Guid.NewGuid(),
            OccurredOnUtc: e.OccurredOnUtc,
            Type: typeof(UserFollowedIntegrationEvent).FullName!,
            Payload: JsonSerializer.Serialize(integrationEvent),
            CorrelationId: correlationId,
            CausationId: causationId);
    }
}

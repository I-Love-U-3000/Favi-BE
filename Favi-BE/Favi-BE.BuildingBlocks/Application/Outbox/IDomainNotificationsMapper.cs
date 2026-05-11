using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Application.Outbox;

/// <summary>
/// Maps a domain event to an OutboxMessageData entry.
/// Returns null to signal "skip this event" (e.g. self-notification guard).
/// </summary>
public interface IDomainNotificationsMapper
{
    Task<OutboxMessageData?> MapAsync(IDomainEvent domainEvent, string? correlationId = null, string? causationId = null);
}

/// <summary>
/// Module-level handler registered with DI (IEnumerable) and consumed by
/// CompositeDomainNotificationsMapper. Each implementation handles a specific set of domain event types.
/// </summary>
public interface IModuleDomainEventMapper
{
    bool CanHandle(IDomainEvent domainEvent);
    Task<OutboxMessageData?> MapAsync(IDomainEvent domainEvent, string? correlationId, string? causationId);
}

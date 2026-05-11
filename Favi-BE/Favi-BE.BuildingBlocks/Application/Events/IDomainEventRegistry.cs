using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Application.Events;

/// <summary>
/// Scoped registry that command handlers use to raise domain events.
/// Events accumulated here are collected by IDomainEventsAccessor and dispatched
/// by TransactionBehavior after the handler completes, inside the same transaction.
/// </summary>
public interface IDomainEventRegistry
{
    void Raise(IDomainEvent domainEvent);
    IReadOnlyList<IDomainEvent> PendingEvents { get; }
    void Clear();
}

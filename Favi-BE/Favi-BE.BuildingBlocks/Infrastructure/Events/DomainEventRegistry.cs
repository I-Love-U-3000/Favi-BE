using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Infrastructure.Events;

public sealed class DomainEventRegistry : IDomainEventRegistry
{
    private readonly List<IDomainEvent> _events = [];

    public void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);
    public IReadOnlyList<IDomainEvent> PendingEvents => _events.AsReadOnly();
    public void Clear() => _events.Clear();
}

using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Application.Events;

public interface IDomainEventsAccessor
{
    IReadOnlyCollection<IDomainEvent> GetAllDomainEvents();
    void ClearAllDomainEvents();
}

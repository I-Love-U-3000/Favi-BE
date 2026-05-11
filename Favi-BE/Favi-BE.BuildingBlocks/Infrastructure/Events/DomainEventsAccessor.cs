using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.BuildingBlocks.Application.Data;
using Favi_BE.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.BuildingBlocks.Infrastructure.Events;

public sealed class DomainEventsAccessor : IDomainEventsAccessor
{
    private readonly IBuildingBlocksDbContext _dbContext;
    private readonly IDomainEventRegistry _registry;

    public DomainEventsAccessor(IBuildingBlocksDbContext dbContext, IDomainEventRegistry registry)
    {
        _dbContext = dbContext;
        _registry = registry;
    }

    public IReadOnlyCollection<IDomainEvent> GetAllDomainEvents()
    {
        var fromEntities = _dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Count > 0)
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        return fromEntities.Concat(_registry.PendingEvents).ToList();
    }

    public void ClearAllDomainEvents()
    {
        var domainEntities = _dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Count > 0)
            .Select(x => x.Entity)
            .ToList();

        foreach (var entity in domainEntities)
            entity.ClearDomainEvents();

        _registry.Clear();
    }
}

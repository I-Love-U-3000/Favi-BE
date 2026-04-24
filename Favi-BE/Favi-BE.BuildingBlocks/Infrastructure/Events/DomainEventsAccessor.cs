using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.BuildingBlocks.Application.Data;
using Favi_BE.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.BuildingBlocks.Infrastructure.Events;

public sealed class DomainEventsAccessor : IDomainEventsAccessor
{
    private readonly IBuildingBlocksDbContext _dbContext;

    public DomainEventsAccessor(IBuildingBlocksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyCollection<IDomainEvent> GetAllDomainEvents()
    {
        var domainEntities = _dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Count > 0)
            .Select(x => x.Entity)
            .ToList();

        return domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();
    }

    public void ClearAllDomainEvents()
    {
        var domainEntities = _dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Count > 0)
            .Select(x => x.Entity)
            .ToList();

        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }
    }
}

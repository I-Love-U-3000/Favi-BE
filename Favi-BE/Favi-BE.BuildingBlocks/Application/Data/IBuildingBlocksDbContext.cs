using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Favi_BE.BuildingBlocks.Application.Data;

public interface IBuildingBlocksDbContext
{
    ChangeTracker ChangeTracker { get; }
    DatabaseFacade Database { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<InboxMessage> InboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Favi_BE.BuildingBlocks.Application.Outbox;
using Favi_BE.BuildingBlocks.Application.Data;

namespace Favi_BE.BuildingBlocks.Infrastructure.Outbox;

public sealed class EfCoreOutbox : IOutbox
{
    private readonly IBuildingBlocksDbContext _dbContext;

    public EfCoreOutbox(IBuildingBlocksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(IEnumerable<OutboxMessageData> messages, CancellationToken cancellationToken = default)
    {
        var entities = messages.Select(x => new OutboxMessage
        {
            Id = x.Id,
            OccurredOnUtc = x.OccurredOnUtc,
            Type = x.Type,
            Payload = x.Payload,
            CorrelationId = x.CorrelationId,
            CausationId = x.CausationId,
            Status = OutboxMessageStatus.Pending,
            Retries = 0
        });

        await _dbContext.OutboxMessages.AddRangeAsync(entities, cancellationToken);
    }
}

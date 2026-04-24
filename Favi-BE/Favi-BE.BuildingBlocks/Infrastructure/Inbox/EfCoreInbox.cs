using Favi_BE.BuildingBlocks.Application.Inbox;
using Favi_BE.BuildingBlocks.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.BuildingBlocks.Infrastructure.Inbox;

public sealed class EfCoreInbox : IInbox
{
    private readonly IBuildingBlocksDbContext _dbContext;

    public EfCoreInbox(IBuildingBlocksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryStartProcessingAsync(string messageId, string consumer, string type, string payload, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.InboxMessages
            .FirstOrDefaultAsync(x => x.MessageId == messageId && x.Consumer == consumer, cancellationToken);

        if (existing is not null)
        {
            return existing.Status != InboxMessageStatus.Processed;
        }

        await _dbContext.InboxMessages.AddAsync(new InboxMessage
        {
            Id = Guid.NewGuid(),
            ReceivedOnUtc = DateTime.UtcNow,
            MessageId = messageId,
            Consumer = consumer,
            Type = type,
            Payload = payload,
            Status = InboxMessageStatus.Processing,
            Retries = 0
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkProcessedAsync(string messageId, string consumer, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.InboxMessages
            .FirstOrDefaultAsync(x => x.MessageId == messageId && x.Consumer == consumer, cancellationToken);

        if (message is null)
        {
            return;
        }

        message.Status = InboxMessageStatus.Processed;
        message.ProcessedOnUtc = DateTime.UtcNow;
        message.Error = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(string messageId, string consumer, string error, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.InboxMessages
            .FirstOrDefaultAsync(x => x.MessageId == messageId && x.Consumer == consumer, cancellationToken);

        if (message is null)
        {
            return;
        }

        message.Retries++;
        message.Error = error;
        message.Status = message.Retries >= 5
            ? InboxMessageStatus.Poisoned
            : InboxMessageStatus.Failed;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

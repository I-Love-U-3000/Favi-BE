using Favi_BE.BuildingBlocks.Application.Data;
using Favi_BE.BuildingBlocks.Application.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Favi_BE.BuildingBlocks.Infrastructure.Inbox;

public sealed class InboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InboxProcessor> _logger;

    public InboxProcessor(IServiceScopeFactory scopeFactory, ILogger<InboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inbox processor failed while processing batch");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IBuildingBlocksDbContext>();
        var consumers = scope.ServiceProvider.GetServices<IInboxConsumer>().ToList();

        var pendingMessages = await dbContext.InboxMessages
            .Where(x => x.Status == InboxMessageStatus.Processing || x.Status == InboxMessageStatus.Failed)
            .OrderBy(x => x.ReceivedOnUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            var consumer = consumers.FirstOrDefault(x => x.MessageType == message.Type);
            if (consumer is null)
            {
                message.Retries++;
                message.Error = $"No inbox consumer registered for message type '{message.Type}'.";
                message.Status = message.Retries >= 5
                    ? InboxMessageStatus.Poisoned
                    : InboxMessageStatus.Failed;
                continue;
            }

            try
            {
                await consumer.HandleAsync(message.Payload, cancellationToken);
                message.Status = InboxMessageStatus.Processed;
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Retries++;
                message.Error = ex.Message;
                message.Status = message.Retries >= 5
                    ? InboxMessageStatus.Poisoned
                    : InboxMessageStatus.Failed;

                _logger.LogError(ex,
                    "Inbox message processing failed. MessageId: {MessageId}, Type: {MessageType}, Retries: {Retries}",
                    message.Id,
                    message.Type,
                    message.Retries);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Favi_BE.BuildingBlocks.Application.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Favi_BE.BuildingBlocks.Infrastructure.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
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
                _logger.LogError(ex, "Outbox processor failed while processing batch");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IBuildingBlocksDbContext>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(x => x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Failed)
            .OrderBy(x => x.OccurredOnUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Retries++;
                message.Error = ex.Message;
                message.Status = message.Retries >= 5
                    ? OutboxMessageStatus.Poisoned
                    : OutboxMessageStatus.Failed;

                _logger.LogError(ex,
                    "Outbox message processing failed. MessageId: {MessageId}, Type: {MessageType}, Retries: {Retries}",
                    message.Id,
                    message.Type,
                    message.Retries);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Favi_BE.BuildingBlocks.Application.Data;
using Favi_BE.BuildingBlocks.Application.Inbox;
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
        var consumers = scope.ServiceProvider.GetServices<IInboxConsumer>().ToList();

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
            using var logScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["OutboxMessageId"] = message.Id,
                ["MessageType"] = message.Type,
                ["CorrelationId"] = message.CorrelationId
            });

            var consumer = consumers.FirstOrDefault(c => c.MessageType == message.Type);

            if (consumer is null)
            {
                // No consumer registered for this message type.
                // Could be an in-process-only domain event already handled by MediatR Publish.
                // Mark as processed to prevent queue clog; log at debug level.
                _logger.LogDebug(
                    "No inbox consumer for OutboxMessageId: {OutboxMessageId}, Type: {MessageType}, CorrelationId: {CorrelationId}. Marking as processed.",
                    message.Id, message.Type, message.CorrelationId);

                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedOnUtc = DateTime.UtcNow;
                continue;
            }

            try
            {
                _logger.LogDebug(
                    "Dispatching OutboxMessageId: {OutboxMessageId}, Type: {MessageType}, CorrelationId: {CorrelationId}",
                    message.Id, message.Type, message.CorrelationId);

                await consumer.HandleAsync(message.Id.ToString(), message.Payload, cancellationToken);

                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;

                _logger.LogInformation(
                    "Outbox message processed. OutboxMessageId: {OutboxMessageId}, Type: {MessageType}, CorrelationId: {CorrelationId}",
                    message.Id, message.Type, message.CorrelationId);
            }
            catch (Exception ex)
            {
                message.Retries++;
                message.Error = ex.Message;
                message.Status = message.Retries >= 5
                    ? OutboxMessageStatus.Poisoned
                    : OutboxMessageStatus.Failed;

                _logger.LogError(ex,
                    "Outbox message processing failed. OutboxMessageId: {OutboxMessageId}, Type: {MessageType}, CorrelationId: {CorrelationId}, Retries: {Retries}",
                    message.Id, message.Type, message.CorrelationId, message.Retries);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

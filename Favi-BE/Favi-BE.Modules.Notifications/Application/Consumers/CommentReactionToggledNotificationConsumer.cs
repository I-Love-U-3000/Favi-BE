using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Inbox;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Favi_BE.Modules.Notifications.Domain;
using Favi_BE.Modules.Notifications.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Modules.Notifications.Application.Consumers;

public sealed class CommentReactionToggledNotificationConsumer : IInboxConsumer
{
    public string MessageType =>
        typeof(CommentReactionToggledIntegrationEvent).FullName
        ?? nameof(CommentReactionToggledIntegrationEvent);

    private readonly IInbox _inbox;
    private readonly INotificationWriteRepository _repository;
    private readonly INotificationRealtimeGateway _gateway;
    private readonly ILogger<CommentReactionToggledNotificationConsumer> _logger;

    public CommentReactionToggledNotificationConsumer(
        IInbox inbox,
        INotificationWriteRepository repository,
        INotificationRealtimeGateway gateway,
        ILogger<CommentReactionToggledNotificationConsumer> logger)
    {
        _inbox = inbox;
        _repository = repository;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task HandleAsync(string messageId, string payload, CancellationToken cancellationToken = default)
    {
        var consumerName = nameof(CommentReactionToggledNotificationConsumer);
        var canProcess = await _inbox.TryStartProcessingAsync(messageId, consumerName, MessageType, payload, cancellationToken);
        if (!canProcess)
        {
            _logger.LogDebug(
                "Inbox dedup: skipping already-processed OutboxMessageId: {MessageId}, Consumer: {Consumer}",
                messageId, consumerName);
            return;
        }

        try
        {
            var @event = JsonSerializer.Deserialize<CommentReactionToggledIntegrationEvent>(payload)
                ?? throw new InvalidOperationException($"Failed to deserialize {nameof(CommentReactionToggledIntegrationEvent)}");

            var notificationId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            var record = new NotificationRecord(
                Id: notificationId,
                Type: NotificationType.Like,
                RecipientProfileId: @event.RecipientId,
                ActorProfileId: @event.ActorId,
                TargetPostId: @event.TargetPostId,
                TargetCommentId: @event.CommentId,
                Message: @event.Message,
                IsRead: false,
                CreatedAt: createdAt);

            await _repository.AddAsync(record, cancellationToken);

            var pushData = new NotificationPushData(
                Id: notificationId,
                Type: NotificationType.Like,
                ActorProfileId: @event.ActorId,
                ActorUsername: @event.ActorUsername,
                ActorDisplayName: @event.ActorDisplayName,
                ActorAvatarUrl: @event.ActorAvatarUrl,
                TargetPostId: @event.TargetPostId,
                TargetCommentId: @event.CommentId,
                Message: @event.Message,
                IsRead: false,
                CreatedAt: createdAt);

            await _gateway.SendNotificationAsync(@event.RecipientId, pushData, cancellationToken);

            var unreadCount = await _repository.GetUnreadCountAsync(@event.RecipientId, cancellationToken);
            await _gateway.SendUnreadCountAsync(@event.RecipientId, unreadCount, cancellationToken);

            await _inbox.MarkProcessedAsync(messageId, consumerName, cancellationToken);

            _logger.LogInformation(
                "Comment reaction notification delivered. OutboxMessageId: {MessageId}, Recipient: {RecipientId}, CommentId: {CommentId}",
                messageId, @event.RecipientId, @event.CommentId);
        }
        catch (Exception ex)
        {
            await _inbox.MarkFailedAsync(messageId, consumerName, ex.Message, cancellationToken);
            _logger.LogError(ex,
                "Comment reaction notification failed. OutboxMessageId: {MessageId}",
                messageId);
            throw;
        }
    }
}

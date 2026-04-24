using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Inbox;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Favi_BE.Modules.Notifications.Domain;
using Favi_BE.Modules.Notifications.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Modules.Notifications.Application.Consumers;

public sealed class CommentCreatedNotificationConsumer : IInboxConsumer
{
    public string MessageType =>
        typeof(CommentCreatedIntegrationEvent).FullName
        ?? nameof(CommentCreatedIntegrationEvent);

    private readonly IInbox _inbox;
    private readonly INotificationWriteRepository _repository;
    private readonly INotificationRealtimeGateway _gateway;
    private readonly ILogger<CommentCreatedNotificationConsumer> _logger;

    public CommentCreatedNotificationConsumer(
        IInbox inbox,
        INotificationWriteRepository repository,
        INotificationRealtimeGateway gateway,
        ILogger<CommentCreatedNotificationConsumer> logger)
    {
        _inbox = inbox;
        _repository = repository;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task HandleAsync(string messageId, string payload, CancellationToken cancellationToken = default)
    {
        var consumerName = nameof(CommentCreatedNotificationConsumer);
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
            var @event = JsonSerializer.Deserialize<CommentCreatedIntegrationEvent>(payload)
                ?? throw new InvalidOperationException($"Failed to deserialize {nameof(CommentCreatedIntegrationEvent)}");

            var notificationId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            var record = new NotificationRecord(
                Id: notificationId,
                Type: NotificationType.Comment,
                RecipientProfileId: @event.RecipientId,
                ActorProfileId: @event.AuthorId,
                TargetPostId: @event.PostId,
                TargetCommentId: @event.CommentId,
                Message: @event.Message,
                IsRead: false,
                CreatedAt: createdAt);

            await _repository.AddAsync(record, cancellationToken);

            var pushData = new NotificationPushData(
                Id: notificationId,
                Type: NotificationType.Comment,
                ActorProfileId: @event.AuthorId,
                ActorUsername: @event.ActorUsername,
                ActorDisplayName: @event.ActorDisplayName,
                ActorAvatarUrl: @event.ActorAvatarUrl,
                TargetPostId: @event.PostId,
                TargetCommentId: @event.CommentId,
                Message: @event.Message,
                IsRead: false,
                CreatedAt: createdAt);

            await _gateway.SendNotificationAsync(@event.RecipientId, pushData, cancellationToken);

            var unreadCount = await _repository.GetUnreadCountAsync(@event.RecipientId, cancellationToken);
            await _gateway.SendUnreadCountAsync(@event.RecipientId, unreadCount, cancellationToken);

            await _inbox.MarkProcessedAsync(messageId, consumerName, cancellationToken);

            _logger.LogInformation(
                "Comment notification delivered. OutboxMessageId: {MessageId}, Recipient: {RecipientId}, CommentId: {CommentId}",
                messageId, @event.RecipientId, @event.CommentId);
        }
        catch (Exception ex)
        {
            await _inbox.MarkFailedAsync(messageId, consumerName, ex.Message, cancellationToken);
            _logger.LogError(ex,
                "Comment notification failed. OutboxMessageId: {MessageId}, Recipient: {RecipientId}",
                messageId, payload.Length > 0 ? "(see payload)" : "unknown");
            throw;
        }
    }
}

using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Inbox;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Favi_BE.Modules.Notifications.Domain;
using Favi_BE.Modules.Notifications.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Modules.Notifications.Application.Consumers;

public sealed class UserFollowedNotificationConsumer : IInboxConsumer
{
    public string MessageType =>
        typeof(UserFollowedIntegrationEvent).FullName
        ?? nameof(UserFollowedIntegrationEvent);

    private readonly IInbox _inbox;
    private readonly INotificationWriteRepository _repository;
    private readonly INotificationRealtimeGateway _gateway;
    private readonly ILogger<UserFollowedNotificationConsumer> _logger;

    public UserFollowedNotificationConsumer(
        IInbox inbox,
        INotificationWriteRepository repository,
        INotificationRealtimeGateway gateway,
        ILogger<UserFollowedNotificationConsumer> logger)
    {
        _inbox = inbox;
        _repository = repository;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task HandleAsync(string messageId, string payload, CancellationToken cancellationToken = default)
    {
        var consumerName = nameof(UserFollowedNotificationConsumer);
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
            var @event = JsonSerializer.Deserialize<UserFollowedIntegrationEvent>(payload)
                ?? throw new InvalidOperationException($"Failed to deserialize {nameof(UserFollowedIntegrationEvent)}");

            var notificationId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            var record = new NotificationRecord(
                Id: notificationId,
                Type: NotificationType.Follow,
                RecipientProfileId: @event.FolloweeId,
                ActorProfileId: @event.FollowerId,
                TargetPostId: null,
                TargetCommentId: null,
                Message: @event.Message,
                IsRead: false,
                CreatedAt: createdAt);

            await _repository.AddAsync(record, cancellationToken);

            var pushData = new NotificationPushData(
                Id: notificationId,
                Type: NotificationType.Follow,
                ActorProfileId: @event.FollowerId,
                ActorUsername: @event.ActorUsername,
                ActorDisplayName: @event.ActorDisplayName,
                ActorAvatarUrl: @event.ActorAvatarUrl,
                TargetPostId: null,
                TargetCommentId: null,
                Message: @event.Message,
                IsRead: false,
                CreatedAt: createdAt);

            await _gateway.SendNotificationAsync(@event.FolloweeId, pushData, cancellationToken);

            var unreadCount = await _repository.GetUnreadCountAsync(@event.FolloweeId, cancellationToken);
            await _gateway.SendUnreadCountAsync(@event.FolloweeId, unreadCount, cancellationToken);

            await _inbox.MarkProcessedAsync(messageId, consumerName, cancellationToken);

            _logger.LogInformation(
                "Follow notification delivered. OutboxMessageId: {MessageId}, Recipient: {RecipientId}, Follower: {FollowerId}",
                messageId, @event.FolloweeId, @event.FollowerId);
        }
        catch (Exception ex)
        {
            await _inbox.MarkFailedAsync(messageId, consumerName, ex.Message, cancellationToken);
            _logger.LogError(ex,
                "Follow notification failed. OutboxMessageId: {MessageId}",
                messageId);
            throw;
        }
    }
}

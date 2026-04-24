using System.Text.Json;
using Favi_BE.API.Hubs;
using Favi_BE.BuildingBlocks.Application;
using Favi_BE.BuildingBlocks.Application.Data;
using Favi_BE.BuildingBlocks.Application.Outbox;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Notifications.Domain.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Services;

/// <summary>
/// Strangler-pattern replacement for NotificationService.
///
/// CREATE side effects (CreateXxxNotificationAsync):
///   Resolves actor/recipient data, then writes an integration event to the Outbox.
///   The OutboxProcessor dispatches it to the matching IInboxConsumer, which persists
///   the Notification entity and pushes SignalR — completely outside any write transaction.
///
/// READ / MARK / DELETE methods:
///   Delegated directly to the underlying infrastructure (same as legacy NotificationService),
///   so the controller API contract is fully preserved.
///
/// Legacy NotificationService.cs is intentionally left in the codebase as rollback fallback.
/// To revert: swap the DI registration in ApplicationExtensions back to NotificationService.
/// </summary>
public sealed class OutboxNotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IOutbox _outbox;
    private readonly IBuildingBlocksDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IExecutionContextAccessor _executionContext;
    private readonly ILogger<OutboxNotificationService> _logger;

    public OutboxNotificationService(
        IUnitOfWork uow,
        IOutbox outbox,
        IBuildingBlocksDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        IExecutionContextAccessor executionContext,
        ILogger<OutboxNotificationService> logger)
    {
        _uow = uow;
        _outbox = outbox;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _executionContext = executionContext;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // CREATE — routed via Outbox (no direct SignalR push in this method)
    // -------------------------------------------------------------------------

    public async Task<NotificationDto?> CreateCommentNotificationAsync(Guid authorId, Guid postId, Guid commentId)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post is null) return null;

        if (post.ProfileId == authorId) return null; // do not notify self

        var actor = await _uow.Profiles.GetByIdAsync(authorId);
        if (actor is null) return null;

        var message = $"{actor.DisplayName ?? actor.Username} commented on your post";

        var integrationEvent = new CommentCreatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            AuthorId: authorId,
            PostId: postId,
            CommentId: commentId,
            RecipientId: post.ProfileId,
            Message: message,
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        await EnqueueOutboxAsync(integrationEvent);
        return null; // eventual consistency: DTO returned to caller is intentionally null
    }

    public async Task<NotificationDto?> CreateFollowNotificationAsync(Guid followerId, Guid followeeId)
    {
        var actor = await _uow.Profiles.GetByIdAsync(followerId);
        if (actor is null) return null;

        var message = $"{actor.DisplayName ?? actor.Username} started following you";

        var integrationEvent = new UserFollowedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            FollowerId: followerId,
            FolloweeId: followeeId,
            Message: message,
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        await EnqueueOutboxAsync(integrationEvent);
        return null;
    }

    public async Task<NotificationDto?> CreatePostReactionNotificationAsync(Guid actorId, Guid postId)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post is null) return null;

        if (post.ProfileId == actorId) return null; // do not notify self

        var actor = await _uow.Profiles.GetByIdAsync(actorId);
        if (actor is null) return null;

        var message = $"{actor.DisplayName ?? actor.Username} reacted to your post";

        var integrationEvent = new PostReactionToggledIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            ActorId: actorId,
            PostId: postId,
            RecipientId: post.ProfileId,
            Message: message,
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        await EnqueueOutboxAsync(integrationEvent);
        return null;
    }

    public async Task<NotificationDto?> CreateCommentReactionNotificationAsync(Guid actorId, Guid commentId)
    {
        var comment = await _uow.Comments.GetByIdAsync(commentId);
        if (comment is null) return null;

        if (comment.ProfileId == actorId) return null; // do not notify self

        var actor = await _uow.Profiles.GetByIdAsync(actorId);
        if (actor is null) return null;

        var message = $"{actor.DisplayName ?? actor.Username} reacted to your comment";

        var integrationEvent = new CommentReactionToggledIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            ActorId: actorId,
            CommentId: commentId,
            TargetPostId: comment.PostId,
            RecipientId: comment.ProfileId,
            Message: message,
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        await EnqueueOutboxAsync(integrationEvent);
        return null;
    }

    // -------------------------------------------------------------------------
    // READ / MARK / DELETE — unchanged from legacy path (no SignalR in transaction)
    // -------------------------------------------------------------------------

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid recipientId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var notifications = await _uow.Notifications.GetNotificationsByRecipientIdAsync(recipientId, skip, pageSize);
        var total = await _uow.Notifications.CountAsync(n => n.RecipientProfileId == recipientId);

        var dtos = notifications.Select(n => MapToDto(n, n.Actor)).ToList();
        return new PagedResult<NotificationDto>(dtos, page, pageSize, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientId)
    {
        return await _uow.Notifications.GetUnreadCountAsync(recipientId);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientId)
    {
        try
        {
            var notification = await _uow.Notifications.GetByIdAsync(notificationId);
            if (notification is null || notification.RecipientProfileId != recipientId) return false;

            notification.IsRead = true;
            _uow.Notifications.Update(notification);
            await _uow.CompleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, recipientId);
            throw;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(Guid recipientId)
    {
        try
        {
            var notifications = await _uow.Notifications.FindAsync(n => n.RecipientProfileId == recipientId && !n.IsRead);
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _uow.Notifications.Update(notification);
            }
            await _uow.CompleteAsync();

            // Direct push acceptable here: this is not a domain write transaction,
            // it is a notification-management operation initiated by the user.
            await _hubContext.Clients.User(recipientId.ToString())
                .SendAsync("UnreadCountUpdated", 0);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", recipientId);
            throw;
        }
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId)
    {
        return await _uow.Notifications.DeleteAsync(notificationId, userId);
    }

    public async Task SendNotificationAsync(Guid recipientId, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.User(recipientId.ToString())
                .SendAsync("ReceiveNotification", notification);

            var unreadCount = await GetUnreadCountAsync(recipientId);
            await _hubContext.Clients.User(recipientId.ToString())
                .SendAsync("UnreadCountUpdated", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {RecipientId}", recipientId);
        }
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private async Task EnqueueOutboxAsync<TEvent>(TEvent integrationEvent) where TEvent : notnull
    {
        var messageType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var payload = JsonSerializer.Serialize(integrationEvent, typeof(TEvent));
        var correlationId = _executionContext.CorrelationId;
        var outboxMessageId = Guid.NewGuid();

        var outboxData = new OutboxMessageData(
            Id: outboxMessageId,
            OccurredOnUtc: DateTime.UtcNow,
            Type: messageType,
            Payload: payload,
            CorrelationId: correlationId,
            CausationId: null);

        await _outbox.AddAsync([outboxData]);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Outbox entry queued. OutboxMessageId: {OutboxMessageId}, Type: {MessageType}, CorrelationId: {CorrelationId}",
            outboxMessageId, messageType, correlationId);
    }

    private static NotificationDto MapToDto(
        Favi_BE.Models.Entities.Notification notification,
        Favi_BE.Models.Entities.Profile? actor)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.ActorProfileId,
            actor?.Username ?? string.Empty,
            actor?.DisplayName,
            actor?.AvatarUrl,
            notification.TargetPostId,
            notification.TargetCommentId,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt
        );
    }
}

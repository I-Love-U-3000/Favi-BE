using Favi_BE.API.Hubs;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork uow,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _uow = uow;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<NotificationDto?> CreatePostReactionNotificationAsync(Guid actorId, Guid postId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return null;

            var actor = await _uow.Profiles.GetByIdAsync(actorId);
            if (actor is null) return null;

            // Don't notify if actor is reacting to their own post
            if (post.ProfileId == actorId) return null;

            var message = $"{actor.DisplayName ?? actor.Username} reacted to your post";

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Like,
                RecipientProfileId = post.ProfileId,
                ActorProfileId = actorId,
                TargetPostId = postId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Notifications.AddAsync(notification);
            await _uow.CompleteAsync();

            var dto = MapToDto(notification, actor);
            await SendNotificationAsync(post.ProfileId, dto);

            return dto;
        }

        public async Task<NotificationDto?> CreateCommentReactionNotificationAsync(Guid actorId, Guid commentId)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null) return null;

            var actor = await _uow.Profiles.GetByIdAsync(actorId);
            if (actor is null) return null;

            // Don't notify if actor is reacting to their own comment
            if (comment.ProfileId == actorId) return null;

            var message = $"{actor.DisplayName ?? actor.Username} reacted to your comment";

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Like,
                RecipientProfileId = comment.ProfileId,
                ActorProfileId = actorId,
                TargetCommentId = commentId,
                TargetPostId = comment.PostId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Notifications.AddAsync(notification);
            await _uow.CompleteAsync();

            var dto = MapToDto(notification, actor);
            await SendNotificationAsync(comment.ProfileId, dto);

            return dto;
        }

        public async Task<NotificationDto?> CreateFollowNotificationAsync(Guid followerId, Guid followeeId)
        {
            var follower = await _uow.Profiles.GetByIdAsync(followerId);
            if (follower is null) return null;

            var message = $"{follower.DisplayName ?? follower.Username} started following you";

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Follow,
                RecipientProfileId = followeeId,
                ActorProfileId = followerId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Notifications.AddAsync(notification);
            await _uow.CompleteAsync();

            var dto = MapToDto(notification, follower);
            await SendNotificationAsync(followeeId, dto);

            return dto;
        }

        public async Task<NotificationDto?> CreateCommentNotificationAsync(Guid authorId, Guid postId, Guid commentId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return null;

            var author = await _uow.Profiles.GetByIdAsync(authorId);
            if (author is null) return null;

            // Don't notify if author is commenting on their own post
            if (post.ProfileId == authorId) return null;

            var message = $"{author.DisplayName ?? author.Username} commented on your post";

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Comment,
                RecipientProfileId = post.ProfileId,
                ActorProfileId = authorId,
                TargetPostId = postId,
                TargetCommentId = commentId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Notifications.AddAsync(notification);
            await _uow.CompleteAsync();

            var dto = MapToDto(notification, author);
            await SendNotificationAsync(post.ProfileId, dto);

            return dto;
        }

        public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid recipientId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;

            var notifications = await _uow.Notifications.GetNotificationsByRecipientIdAsync(recipientId, skip, pageSize);
            var total = await _uow.Notifications.CountAsync(n => n.RecipientProfileId == recipientId);

            var dtos = notifications.Select(n =>
            {
                var actor = n.Actor;
                return MapToDto(n, actor);
            }).ToList();

            return new PagedResult<NotificationDto>(dtos, page, pageSize, total);
        }

        public async Task<int> GetUnreadCountAsync(Guid recipientId)
        {
            return await _uow.Notifications.GetUnreadCountAsync(recipientId);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientId)
        {
            var notification = await _uow.Notifications.GetByIdAsync(notificationId);
            if (notification is null || notification.RecipientProfileId != recipientId) return false;

            notification.IsRead = true;
            _uow.Notifications.Update(notification);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid recipientId)
        {
            var notifications = await _uow.Notifications.FindAsync(n => n.RecipientProfileId == recipientId && !n.IsRead);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _uow.Notifications.Update(notification);
            }

            await _uow.CompleteAsync();

            // Send updated unread count via SignalR
            await _hubContext.Clients.User(recipientId.ToString())
                .SendAsync("UnreadCountUpdated", 0);

            return true;
        }

        public async Task SendNotificationAsync(Guid recipientId, NotificationDto notification)
        {
            try
            {
                await _hubContext.Clients.User(recipientId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                // Also update unread count
                var unreadCount = await GetUnreadCountAsync(recipientId);
                await _hubContext.Clients.User(recipientId.ToString())
                    .SendAsync("UnreadCountUpdated", unreadCount);

                _logger.LogInformation(
                    "Notification sent to user {RecipientId}: {Type} - {Message}",
                    recipientId, notification.Type, notification.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {RecipientId}", recipientId);
            }
        }

        private static NotificationDto MapToDto(Notification notification, Profile? actor)
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
}

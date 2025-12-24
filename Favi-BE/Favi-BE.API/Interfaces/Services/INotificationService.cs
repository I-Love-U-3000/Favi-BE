using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Services
{
    public interface INotificationService
    {
        // Create and send notifications
        Task<NotificationDto?> CreatePostReactionNotificationAsync(Guid actorId, Guid postId);
        Task<NotificationDto?> CreateCommentReactionNotificationAsync(Guid actorId, Guid commentId);
        Task<NotificationDto?> CreateFollowNotificationAsync(Guid followerId, Guid followeeId);
        Task<NotificationDto?> CreateCommentNotificationAsync(Guid authorId, Guid postId, Guid commentId);

        // Query notifications
        Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid recipientId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(Guid recipientId);

        // Mark notifications
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientId);
        Task<bool> MarkAllAsReadAsync(Guid recipientId);

        // Send real-time notification (via SignalR)
        Task SendNotificationAsync(Guid recipientId, NotificationDto notification);
    }
}

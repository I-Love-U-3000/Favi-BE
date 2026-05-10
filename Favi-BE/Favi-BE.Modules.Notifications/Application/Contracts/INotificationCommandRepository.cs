namespace Favi_BE.Modules.Notifications.Application.Contracts;

public interface INotificationCommandRepository
{
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken = default);
}

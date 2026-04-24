namespace Favi_BE.Modules.Notifications.Application.Contracts;

/// <summary>
/// Port: write operations for the Notification persistence side.
/// Implemented by an adapter in Favi-BE.API that wraps the EF repository.
/// </summary>
public interface INotificationWriteRepository
{
    Task AddAsync(NotificationRecord notification, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default);
}

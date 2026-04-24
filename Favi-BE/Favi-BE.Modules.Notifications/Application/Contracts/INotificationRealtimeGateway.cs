namespace Favi_BE.Modules.Notifications.Application.Contracts;

/// <summary>
/// Port: real-time delivery of notification events to connected clients.
/// Implemented by an adapter in Favi-BE.API that wraps IHubContext&lt;NotificationHub&gt;.
/// Preserves client-visible event names: ReceiveNotification, UnreadCountUpdated.
/// </summary>
public interface INotificationRealtimeGateway
{
    Task SendNotificationAsync(Guid recipientId, NotificationPushData data, CancellationToken cancellationToken = default);
    Task SendUnreadCountAsync(Guid recipientId, int count, CancellationToken cancellationToken = default);
}

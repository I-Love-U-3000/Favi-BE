using Favi_BE.Modules.Notifications.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Notifications.Application.Contracts;

public interface INotificationQueryReader
{
    Task<(IReadOnlyList<NotificationReadModel> Items, int TotalCount)> GetNotificationsAsync(
        Guid recipientId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default);
}

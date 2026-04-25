using Favi_BE.Interfaces.Services;
using Favi_BE.Modules.SocialGraph.Application.Contracts;

namespace Favi_BE.API.Application.SocialGraph;

internal sealed class SocialGraphNotificationServiceAdapter : ISocialGraphNotificationService
{
    private readonly INotificationService _notifications;

    public SocialGraphNotificationServiceAdapter(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task NotifyUserFollowedAsync(Guid followerId, Guid followeeId, CancellationToken ct = default)
        => await _notifications.CreateFollowNotificationAsync(followerId, followeeId);
}

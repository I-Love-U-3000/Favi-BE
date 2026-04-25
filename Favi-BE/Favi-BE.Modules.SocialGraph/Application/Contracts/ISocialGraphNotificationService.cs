namespace Favi_BE.Modules.SocialGraph.Application.Contracts;

public interface ISocialGraphNotificationService
{
    Task NotifyUserFollowedAsync(Guid followerId, Guid followeeId, CancellationToken ct = default);
}

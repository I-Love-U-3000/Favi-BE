using Favi_BE.Interfaces.Services;
using Favi_BE.Modules.Engagement.Application.Contracts;

namespace Favi_BE.API.Application.Engagement;

/// <summary>
/// Routes Engagement module notification requests to the existing INotificationService infrastructure.
/// Keeps the Engagement module decoupled from Notifications module internals.
/// </summary>
internal sealed class EngagementNotificationServiceAdapter : IEngagementNotificationService
{
    private readonly INotificationService _notifications;

    public EngagementNotificationServiceAdapter(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task NotifyCommentCreatedAsync(Guid authorId, Guid postId, Guid commentId, CancellationToken ct = default)
        => await _notifications.CreateCommentNotificationAsync(authorId, postId, commentId);

    public async Task NotifyPostReactionAddedAsync(Guid actorId, Guid postId, CancellationToken ct = default)
        => await _notifications.CreatePostReactionNotificationAsync(actorId, postId);

    public async Task NotifyCommentReactionAddedAsync(Guid actorId, Guid commentId, CancellationToken ct = default)
        => await _notifications.CreateCommentReactionNotificationAsync(actorId, commentId);
}

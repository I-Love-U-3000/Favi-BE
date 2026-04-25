namespace Favi_BE.Modules.Engagement.Application.Contracts;

public interface IEngagementNotificationService
{
    Task NotifyCommentCreatedAsync(Guid authorId, Guid postId, Guid commentId, CancellationToken ct = default);
    Task NotifyPostReactionAddedAsync(Guid actorId, Guid postId, CancellationToken ct = default);
    Task NotifyCommentReactionAddedAsync(Guid actorId, Guid commentId, CancellationToken ct = default);
}

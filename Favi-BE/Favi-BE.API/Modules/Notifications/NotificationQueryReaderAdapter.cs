using Favi_BE.Data;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Favi_BE.Modules.Notifications.Application.Contracts.ReadModels;
using Favi_BE.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Modules.Notifications;

public sealed class NotificationQueryReaderAdapter : INotificationQueryReader
{
    private readonly AppDbContext _dbContext;

    public NotificationQueryReaderAdapter(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<(IReadOnlyList<NotificationReadModel> Items, int TotalCount)> GetNotificationsAsync(
        Guid recipientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientProfileId == recipientId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(n => n.Actor)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(n => new NotificationReadModel(
                n.Id,
                MapType(n.Type),
                n.RecipientProfileId,
                n.ActorProfileId,
                n.Actor.Username,
                n.Actor.DisplayName,
                n.Actor.AvatarUrl,
                n.TargetPostId,
                n.TargetCommentId,
                n.Message,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default)
        => _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientProfileId == recipientId && !n.IsRead, cancellationToken);

    private static NotificationType MapType(Favi_BE.Models.Enums.NotificationType type) => type switch
    {
        Favi_BE.Models.Enums.NotificationType.Like => NotificationType.Like,
        Favi_BE.Models.Enums.NotificationType.Comment => NotificationType.Comment,
        Favi_BE.Models.Enums.NotificationType.Follow => NotificationType.Follow,
        Favi_BE.Models.Enums.NotificationType.Share => NotificationType.Share,
        _ => NotificationType.System
    };
}

using Favi_BE.Data;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Modules.Notifications;

public sealed class NotificationCommandRepositoryAdapter : INotificationCommandRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationCommandRepositoryAdapter(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientProfileId == recipientId, cancellationToken);

        if (notification is null) return false;

        notification.IsRead = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.Notifications
            .Where(n => n.RecipientProfileId == recipientId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in notifications)
            n.IsRead = true;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientProfileId == recipientId, cancellationToken);

        if (notification is null) return false;

        _dbContext.Notifications.Remove(notification);
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}

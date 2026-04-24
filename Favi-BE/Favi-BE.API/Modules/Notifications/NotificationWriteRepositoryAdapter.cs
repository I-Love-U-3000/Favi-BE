using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Modules.Notifications;

/// <summary>
/// Adapter: implements INotificationWriteRepository (port defined in Modules.Notifications)
/// using the EF Core AppDbContext from the API layer.
/// </summary>
public sealed class NotificationWriteRepositoryAdapter : INotificationWriteRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationWriteRepositoryAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        var entity = new Notification
        {
            Id = record.Id,
            Type = MapType(record.Type),
            RecipientProfileId = record.RecipientProfileId,
            ActorProfileId = record.ActorProfileId,
            TargetPostId = record.TargetPostId,
            TargetCommentId = record.TargetCommentId,
            Message = record.Message,
            IsRead = record.IsRead,
            CreatedAt = record.CreatedAt
        };

        await _dbContext.Notifications.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .CountAsync(n => n.RecipientProfileId == recipientId && !n.IsRead, cancellationToken);
    }

    private static Favi_BE.Models.Enums.NotificationType MapType(
        Favi_BE.Modules.Notifications.Domain.NotificationType moduleType)
    {
        return moduleType switch
        {
            Favi_BE.Modules.Notifications.Domain.NotificationType.Like => Favi_BE.Models.Enums.NotificationType.Like,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Comment => Favi_BE.Models.Enums.NotificationType.Comment,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Follow => Favi_BE.Models.Enums.NotificationType.Follow,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Share => Favi_BE.Models.Enums.NotificationType.Share,
            _ => Favi_BE.Models.Enums.NotificationType.System
        };
    }
}

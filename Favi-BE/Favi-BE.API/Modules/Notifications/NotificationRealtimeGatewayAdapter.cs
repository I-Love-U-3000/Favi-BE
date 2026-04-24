using Favi_BE.API.Hubs;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Favi_BE.API.Modules.Notifications;

/// <summary>
/// Adapter: implements INotificationRealtimeGateway (port defined in Modules.Notifications)
/// using IHubContext&lt;NotificationHub&gt; from the API layer.
/// Preserves client-visible event names: ReceiveNotification, UnreadCountUpdated.
/// </summary>
public sealed class NotificationRealtimeGatewayAdapter : INotificationRealtimeGateway
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationRealtimeGatewayAdapter> _logger;

    public NotificationRealtimeGatewayAdapter(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationRealtimeGatewayAdapter> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Guid recipientId, NotificationPushData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = MapToDto(data);
            await _hubContext.Clients.User(recipientId.ToString())
                .SendAsync("ReceiveNotification", dto, cancellationToken);

            _logger.LogInformation(
                "SignalR ReceiveNotification sent to user {RecipientId}: {Type} - {Message}",
                recipientId, data.Type, data.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ReceiveNotification to user {RecipientId}", recipientId);
        }
    }

    public async Task SendUnreadCountAsync(Guid recipientId, int count, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.User(recipientId.ToString())
                .SendAsync("UnreadCountUpdated", count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending UnreadCountUpdated to user {RecipientId}", recipientId);
        }
    }

    private static NotificationDto MapToDto(NotificationPushData data)
    {
        return new NotificationDto(
            data.Id,
            MapType(data.Type),
            data.ActorProfileId,
            data.ActorUsername,
            data.ActorDisplayName,
            data.ActorAvatarUrl,
            data.TargetPostId,
            data.TargetCommentId,
            data.Message,
            data.IsRead,
            data.CreatedAt
        );
    }

    private static NotificationType MapType(
        Favi_BE.Modules.Notifications.Domain.NotificationType moduleType)
    {
        return moduleType switch
        {
            Favi_BE.Modules.Notifications.Domain.NotificationType.Like => NotificationType.Like,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Comment => NotificationType.Comment,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Follow => NotificationType.Follow,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Share => NotificationType.Share,
            _ => NotificationType.System
        };
    }
}

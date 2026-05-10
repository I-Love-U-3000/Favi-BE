using Favi_BE.Common;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Notifications.Application.Commands.DeleteNotification;
using Favi_BE.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;
using Favi_BE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;
using Favi_BE.Modules.Notifications.Application.Contracts.ReadModels;
using Favi_BE.Modules.Notifications.Application.Queries.GetNotifications;
using Favi_BE.Modules.Notifications.Application.Queries.GetUnreadNotificationCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var (items, total) = await _mediator.Send(new GetNotificationsQuery(userId, page, pageSize));
            var dtos = items.Select(MapToDto).ToList();
            return Ok(new PagedResult<NotificationDto>(dtos, page, pageSize, total));
        }

        [Authorize]
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = User.GetUserId();
            var count = await _mediator.Send(new GetUnreadNotificationCountQuery(userId));
            return Ok(count);
        }

        [Authorize]
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = User.GetUserId();
            var success = await _mediator.Send(new MarkNotificationAsReadCommand(id, userId));

            return success
                ? Ok(new { message = "Notification marked as read." })
                : NotFound(new { code = "NOTIFICATION_NOT_FOUND", message = "Notification not found or access denied." });
        }

        [Authorize]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            await _mediator.Send(new MarkAllNotificationsAsReadCommand(userId));
            return Ok(new { message = "All notifications marked as read." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(Guid id)
        {
            var userId = User.GetUserId();
            var success = await _mediator.Send(new DeleteNotificationCommand(id, userId));
            if (!success) return NotFound();
            return NoContent();
        }

        private static NotificationDto MapToDto(NotificationReadModel n) => new(
            n.Id,
            MapType(n.Type),
            n.ActorProfileId,
            n.ActorUsername,
            n.ActorDisplayName,
            n.ActorAvatarUrl,
            n.TargetPostId,
            n.TargetCommentId,
            n.Message,
            n.IsRead,
            n.CreatedAt);

        private static NotificationType MapType(Favi_BE.Modules.Notifications.Domain.NotificationType type) => type switch
        {
            Favi_BE.Modules.Notifications.Domain.NotificationType.Like => NotificationType.Like,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Comment => NotificationType.Comment,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Follow => NotificationType.Follow,
            Favi_BE.Modules.Notifications.Domain.NotificationType.Share => NotificationType.Share,
            _ => NotificationType.System
        };
    }
}

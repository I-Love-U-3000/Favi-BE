using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifications;

        public NotificationsController(INotificationService notifications)
        {
            _notifications = notifications;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            return Ok(await _notifications.GetNotificationsAsync(userId, page, pageSize));
        }

        [Authorize]
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = User.GetUserId();
            return Ok(await _notifications.GetUnreadCountAsync(userId));
        }

        [Authorize]
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = User.GetUserId();
            var success = await _notifications.MarkAsReadAsync(id, userId);

            return success
                ? Ok(new { message = "Notification marked as read." })
                : NotFound(new { code = "NOTIFICATION_NOT_FOUND", message = "Notification not found or access denied." });
        }

        [Authorize]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            await _notifications.MarkAllAsReadAsync(userId);
            return Ok(new { message = "All notifications marked as read." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(Guid id)
        {
            var userId = User.GetUserId();
            var success = await _notifications.DeleteNotificationAsync(id, userId);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}

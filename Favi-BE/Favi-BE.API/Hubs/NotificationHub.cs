using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Favi_BE.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation("User {UserId} connected to notification hub", userId);

                // Add user to their personal notification group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation("User {UserId} disconnected from notification hub", userId);

                // Remove user from their personal notification group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}

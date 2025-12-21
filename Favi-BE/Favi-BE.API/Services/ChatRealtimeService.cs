using Favi_BE.API.Models.Dtos;
using Favi_BE.API.Interfaces.Services;
using Favi_BE.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Favi_BE.API.Services
{
    public class ChatRealtimeService : IChatRealtimeService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatRealtimeService> _logger;

        public ChatRealtimeService(IHubContext<ChatHub> hubContext, ILogger<ChatRealtimeService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyConversationUpdatedAsync(Guid conversationId, object notification)
        {
            await _hubContext.Clients.Group(conversationId.ToString()).SendAsync("ConversationUpdated", notification);
        }

        public async Task NotifyMessageSentAsync(Guid conversationId, MessageDto message)
        {
            await _hubContext.Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message);
        }

        public async Task NotifyUserJoinedAsync(Guid conversationId, Guid userId)
        {
            await _hubContext.Clients
                .Group(conversationId.ToString())
                .SendAsync("UserJoined", new { conversationId, userId });
        }

        public async Task NotifyUserLeftAsync(Guid conversationId, Guid userId)
        {
            await _hubContext.Clients
                .Group(conversationId.ToString())
                .SendAsync("UserLeft", new { conversationId, userId });
        }

        public async Task NotifyMessageReadAsync(Guid conversationId, Guid userId, Guid messageId)
        {
            await _hubContext.Clients
                .Group(conversationId.ToString())
                .SendAsync("MessageRead", new { conversationId, userId, messageId });
        }
    }
}
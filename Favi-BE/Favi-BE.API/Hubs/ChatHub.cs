using Favi_BE.API.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Favi_BE.API.Models.Entities;
using Favi_BE.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace Favi_BE.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IChatService _chatService;
        private readonly IServiceProvider _serviceProvider;

        public ChatHub(ILogger<ChatHub> logger, IChatService chatService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _chatService = chatService;
            _serviceProvider = serviceProvider;
        }

        public async Task JoinConversation(string conversationId)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    // Verify user has access to conversation
                    var conversations = await _chatService.GetConversationsAsync(userId, 1, 1);
                    var hasAccess = conversations.Any(c => c.Id == convId);

                    if (hasAccess)
                    {
                        var groupName = convId.ToString(); // hoặc conversationId cũng được, miễn nhất quán

                        // ✅ Đúng: truyền connectionId + groupName
                        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                        _logger.LogInformation($"User {userId} joined conversation {convId}");

                        // Notify others in group
                        await Clients.OthersInGroup(groupName)
                            .SendAsync("UserJoined", new { conversationId = convId, userId });
                    }
                    else
                    {
                        _logger.LogWarning($"User {userId} denied access to conversation {convId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error joining conversation {convId} for user {userId}");
                }
            }
        }

        public async Task LeaveConversation(string conversationId)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    var groupName = convId.ToString();

                    // ✅ Đúng: truyền connectionId + groupName
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                    // Notify others in group
                    await Clients.OthersInGroup(groupName)
                        .SendAsync("UserLeft", new { conversationId = convId, userId });

                    _logger.LogInformation($"User {userId} left conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error leaving conversation {convId} for user {userId}");
                }
            }
        }

        public async Task SendMessageToConversation(string conversationId, SendMessageRequest request)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    // Create message through service
                    var messageDto = await _chatService.SendMessageAsync(userId, convId, request);
                    
                    // Broadcast to all users in the conversation
                    await Clients.Group(conversationId).SendAsync("ReceiveMessage", messageDto);

                    _logger.LogInformation($"Message sent in conversation {convId} by user {userId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending message in conversation {convId} by user {userId}");
                    await Clients.Caller.SendAsync("SendMessageError", new { conversationId = convId, error = "Failed to send message" });
                }
            }
        }

        public async Task MarkAsRead(string conversationId, string messageId)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && 
                Guid.TryParse(conversationId, out var convId) && 
                Guid.TryParse(messageId, out var msgId))
            {
                try
                {
                    await _chatService.MarkAsReadAsync(userId, convId, msgId);
                    
                    // Notify others in the conversation that this user read the message
                    await Clients.OthersInGroup(conversationId).SendAsync("MessageRead", new { conversationId = convId, userId = userId, messageId = msgId });
                    
                    _logger.LogInformation($"User {userId} marked message {msgId} as read in conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error marking message as read in conversation {convId} by user {userId}");
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation($"User {userId} connected to chat hub");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation($"User {userId} disconnected from chat hub");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;
using Favi_BE.Modules.Messaging.Application.Commands.MarkConversationRead;
using Favi_BE.Modules.Messaging.Application.Commands.SendMessage;
using Favi_BE.Modules.Messaging.Application.Queries.GetConversations;
using Favi_BE.API.Models.Dtos;
using MediatR;

namespace Favi_BE.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IMediator _mediator;

        public ChatHub(ILogger<ChatHub> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task JoinConversation(string conversationId)
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId) ||
                !Guid.TryParse(conversationId, out var convId))
                return;

            try
            {
                var conversations = await _mediator.Send(new GetConversationsQuery(userId, 1, int.MaxValue));
                if (!conversations.Any(c => c.Id == convId))
                {
                    _logger.LogWarning("User {UserId} denied access to conversation {ConvId}", userId, convId);
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, convId.ToString());
                await Clients.OthersInGroup(convId.ToString())
                    .SendAsync("UserJoined", new { conversationId = convId, userId });

                _logger.LogInformation("User {UserId} joined conversation {ConvId}", userId, convId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining conversation {ConvId} for user {UserId}", convId, userId);
            }
        }

        public async Task LeaveConversation(string conversationId)
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId) ||
                !Guid.TryParse(conversationId, out var convId))
                return;

            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, convId.ToString());
                await Clients.OthersInGroup(convId.ToString())
                    .SendAsync("UserLeft", new { conversationId = convId, userId });

                _logger.LogInformation("User {UserId} left conversation {ConvId}", userId, convId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving conversation {ConvId} for user {UserId}", convId, userId);
            }
        }

        public async Task SendMessageToConversation(string conversationId, SendMessageRequest request)
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId) ||
                !Guid.TryParse(conversationId, out var convId))
                return;

            try
            {
                await _mediator.Send(new SendMessageCommand(userId, convId, request.Content, request.MediaUrl, request.PostId));
                _logger.LogInformation("Message sent in conversation {ConvId} by user {UserId}", convId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message in conversation {ConvId} by user {UserId}", convId, userId);
                await Clients.Caller.SendAsync("SendMessageError", new { conversationId = convId, error = "Failed to send message" });
            }
        }

        public async Task MarkAsRead(string conversationId, string messageId)
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId) ||
                !Guid.TryParse(conversationId, out var convId) ||
                !Guid.TryParse(messageId, out var msgId))
                return;

            try
            {
                await _mediator.Send(new MarkConversationReadCommand(userId, convId, msgId));
                _logger.LogInformation("User {UserId} marked message {MsgId} as read in conversation {ConvId}", userId, msgId, convId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read in conversation {ConvId} by user {UserId}", convId, userId);
            }
        }

        public override async Task OnConnectedAsync()
        {
            if (Guid.TryParse(Context.UserIdentifier, out var userId))
            {
                _logger.LogInformation("User {UserId} connected to chat hub", userId);
                try
                {
                    await _mediator.Send(new UpdateLastActiveCommand(userId));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update last active for user {UserId}", userId);
                }
            }

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (Guid.TryParse(Context.UserIdentifier, out var userId))
                _logger.LogInformation("User {UserId} disconnected from chat hub", userId);

            return base.OnDisconnectedAsync(exception);
        }
    }
}

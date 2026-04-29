using Favi_BE.API.Hubs;
using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using Microsoft.AspNetCore.SignalR;

namespace Favi_BE.API.Application.Messaging;

internal sealed class ChatRealtimeGatewayAdapter : IChatRealtimeGateway
{
    private readonly IHubContext<ChatHub> _hub;

    public ChatRealtimeGatewayAdapter(IHubContext<ChatHub> hub) => _hub = hub;

    public Task NotifyMessageSentAsync(Guid conversationId, MessageReadModel message, CancellationToken ct = default)
        => _hub.Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message, ct);

    public Task NotifyMessageReadAsync(Guid conversationId, Guid userId, Guid messageId, CancellationToken ct = default)
        => _hub.Clients.Group(conversationId.ToString())
            .SendAsync("MessageRead", new { conversationId, userId, messageId }, ct);
}

using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Messaging.Application.Contracts;

/// <summary>
/// Port: real-time delivery of chat events to connected clients via SignalR.
/// Implemented by an adapter in Favi-BE.API that wraps IHubContext&lt;ChatHub&gt;.
/// Must be called AFTER SaveAsync to avoid in-transaction hub push.
/// </summary>
public interface IChatRealtimeGateway
{
    Task NotifyMessageSentAsync(Guid conversationId, MessageReadModel message, CancellationToken ct = default);
    Task NotifyMessageReadAsync(Guid conversationId, Guid userId, Guid messageId, CancellationToken ct = default);
}

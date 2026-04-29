using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;
using MediatR;

namespace Favi_BE.Modules.Messaging.Application.Commands.SendMessage;

internal sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageReadModel?>
{
    private readonly IMessagingCommandRepository _repo;
    private readonly IChatRealtimeGateway _realtime;

    public SendMessageCommandHandler(IMessagingCommandRepository repo, IChatRealtimeGateway realtime)
    {
        _repo = repo;
        _realtime = realtime;
    }

    public async Task<MessageReadModel?> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var participant = await _repo.GetParticipantAsync(request.ConversationId, request.SenderProfileId, cancellationToken);
        if (participant is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.Content)
            && string.IsNullOrWhiteSpace(request.MediaUrl)
            && request.PostId is null)
            return null;

        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await _repo.AddMessageAsync(
            new MessageWriteData(
                messageId,
                request.ConversationId,
                request.SenderProfileId,
                request.Content?.Trim(),
                request.MediaUrl?.Trim(),
                request.PostId,
                now),
            cancellationToken);

        await _repo.SetConversationLastMessageAtAsync(request.ConversationId, now, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        var message = await _repo.GetMessageAsync(messageId, cancellationToken);
        if (message is not null)
            await _realtime.NotifyMessageSentAsync(request.ConversationId, message, cancellationToken);

        return message;
    }
}

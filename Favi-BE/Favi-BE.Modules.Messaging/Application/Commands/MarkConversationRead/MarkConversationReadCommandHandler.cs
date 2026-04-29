using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Messaging.Application.Commands.MarkConversationRead;

internal sealed class MarkConversationReadCommandHandler : IRequestHandler<MarkConversationReadCommand, MessagingCommandResult>
{
    private readonly IMessagingCommandRepository _repo;
    private readonly IChatRealtimeGateway _realtime;

    public MarkConversationReadCommandHandler(IMessagingCommandRepository repo, IChatRealtimeGateway realtime)
    {
        _repo = repo;
        _realtime = realtime;
    }

    public async Task<MessagingCommandResult> Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
    {
        var participant = await _repo.GetParticipantAsync(request.ConversationId, request.ProfileId, cancellationToken);
        if (participant is null)
            return MessagingCommandResult.Fail("ACCESS_DENIED", "Access denied to conversation.");

        await _repo.SetLastReadMessageAsync(request.ConversationId, request.ProfileId, request.LastMessageId, cancellationToken);
        await _repo.MarkMessageReadAsync(request.LastMessageId, request.ProfileId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        await _realtime.NotifyMessageReadAsync(request.ConversationId, request.ProfileId, request.LastMessageId, cancellationToken);

        return MessagingCommandResult.Success();
    }
}

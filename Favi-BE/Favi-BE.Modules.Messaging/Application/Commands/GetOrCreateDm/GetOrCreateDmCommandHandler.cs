using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;
using Favi_BE.Modules.Messaging.Domain;
using MediatR;

namespace Favi_BE.Modules.Messaging.Application.Commands.GetOrCreateDm;

internal sealed class GetOrCreateDmCommandHandler : IRequestHandler<GetOrCreateDmCommand, ConversationSummaryReadModel?>
{
    private readonly IMessagingCommandRepository _repo;

    public GetOrCreateDmCommandHandler(IMessagingCommandRepository repo) => _repo = repo;

    public async Task<ConversationSummaryReadModel?> Handle(GetOrCreateDmCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.ProfileExistsAsync(request.OtherProfileId, cancellationToken))
            return null;

        var existing = await _repo.FindDmConversationAsync(request.CurrentProfileId, request.OtherProfileId, cancellationToken);
        if (existing is not null)
            return await _repo.GetConversationSummaryAsync(existing.Id, request.CurrentProfileId, cancellationToken);

        var conversationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await _repo.AddConversationAsync(
            new ConversationWriteData(conversationId, ConversationType.Dm, now, null),
            cancellationToken);

        await _repo.AddParticipantsAsync(
            [
                new ConversationParticipantData(conversationId, request.CurrentProfileId, "member", now),
                new ConversationParticipantData(conversationId, request.OtherProfileId, "member", now)
            ],
            cancellationToken);

        await _repo.SaveAsync(cancellationToken);

        return await _repo.GetConversationSummaryAsync(conversationId, request.CurrentProfileId, cancellationToken);
    }
}

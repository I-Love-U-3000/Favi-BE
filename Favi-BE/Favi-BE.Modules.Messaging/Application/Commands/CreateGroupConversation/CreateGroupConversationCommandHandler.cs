using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;
using Favi_BE.Modules.Messaging.Domain;
using MediatR;

namespace Favi_BE.Modules.Messaging.Application.Commands.CreateGroupConversation;

internal sealed class CreateGroupConversationCommandHandler : IRequestHandler<CreateGroupConversationCommand, ConversationSummaryReadModel?>
{
    private readonly IMessagingCommandRepository _repo;

    public CreateGroupConversationCommandHandler(IMessagingCommandRepository repo) => _repo = repo;

    public async Task<ConversationSummaryReadModel?> Handle(CreateGroupConversationCommand request, CancellationToken cancellationToken)
    {
        var distinctMembers = request.MemberIds
            .Distinct()
            .Where(id => id != request.CreatorProfileId)
            .ToList();

        if (distinctMembers.Count == 0)
            return null;

        foreach (var memberId in distinctMembers)
        {
            if (!await _repo.ProfileExistsAsync(memberId, cancellationToken))
                return null;
        }

        var conversationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await _repo.AddConversationAsync(
            new ConversationWriteData(conversationId, ConversationType.Group, now, null),
            cancellationToken);

        var participants = new List<ConversationParticipantData>
        {
            new(conversationId, request.CreatorProfileId, "owner", now)
        };
        participants.AddRange(distinctMembers.Select(id =>
            new ConversationParticipantData(conversationId, id, "member", now)));

        await _repo.AddParticipantsAsync(participants, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return await _repo.GetConversationSummaryAsync(conversationId, request.CreatorProfileId, cancellationToken);
    }
}

using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;

namespace Favi_BE.Modules.Messaging.Application.Contracts;

public interface IMessagingCommandRepository
{
    // Conversation write
    Task<ConversationWriteData?> FindDmConversationAsync(Guid profileA, Guid profileB, CancellationToken ct = default);
    Task AddConversationAsync(ConversationWriteData data, CancellationToken ct = default);
    Task SetConversationLastMessageAtAsync(Guid conversationId, DateTime lastMessageAt, CancellationToken ct = default);

    // Participants
    Task AddParticipantsAsync(IReadOnlyList<ConversationParticipantData> participants, CancellationToken ct = default);
    Task<ConversationParticipantData?> GetParticipantAsync(Guid conversationId, Guid profileId, CancellationToken ct = default);
    Task SetLastReadMessageAsync(Guid conversationId, Guid profileId, Guid lastMessageId, CancellationToken ct = default);

    // Message write
    Task AddMessageAsync(MessageWriteData data, CancellationToken ct = default);
    Task MarkMessageReadAsync(Guid messageId, Guid profileId, CancellationToken ct = default);

    // Post-write response construction (not for general querying)
    Task<ConversationSummaryReadModel?> GetConversationSummaryAsync(Guid conversationId, Guid requestingProfileId, CancellationToken ct = default);
    Task<MessageReadModel?> GetMessageAsync(Guid messageId, CancellationToken ct = default);

    // Cross-context business rule read
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);
}

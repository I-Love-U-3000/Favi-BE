using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Messaging.Application.Contracts;

public interface IMessagingQueryReader
{
    Task<IReadOnlyList<ConversationSummaryReadModel>> GetConversationsAsync(Guid profileId, int skip, int take, CancellationToken ct = default);
    Task<(IReadOnlyList<MessageReadModel> Items, int Total)> GetMessagesAsync(Guid conversationId, Guid requestingProfileId, int skip, int take, CancellationToken ct = default);
    Task<int> GetUnreadMessagesCountAsync(Guid profileId, CancellationToken ct = default);
}

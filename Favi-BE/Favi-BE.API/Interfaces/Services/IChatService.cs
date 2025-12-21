using Favi_BE.API.Models.Dtos;

namespace Favi_BE.API.Interfaces.Services
{
    public interface IChatService
    {
        Task<ConversationSummaryDto> GetOrCreateDmAsync(Guid currentProfileId, Guid otherProfileId);
        Task<ConversationSummaryDto> CreateGroupAsync(Guid currentProfileId, CreateGroupConversationRequest dto);
        Task<IEnumerable<ConversationSummaryDto>> GetConversationsAsync(Guid currentProfileId, int page, int pageSize);

        Task<(IEnumerable<MessageDto> Items, int Total)> GetMessagesAsync(
            Guid currentProfileId, Guid conversationId, int page, int pageSize);

        Task<MessageDto> SendMessageAsync(
            Guid currentProfileId, Guid conversationId, SendMessageRequest dto);

        Task MarkAsReadAsync(Guid currentProfileId, Guid conversationId, Guid lastMessageId);

        Task UpdateUserLastActiveAsync(Guid userId);
    }
}

using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Interfaces.Repositories;

namespace Favi_BE.API.Interfaces.Repositories
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<(IEnumerable<Message> Items, int Total)> GetMessagesForConversationAsync(Guid conversationId, int skip, int take);

        Task<Message?> GetLastMessageAsync(Guid conversationId);

        Task<int> GetUnreadCountAsync(Guid conversationId, DateTime afterTime);

        // Message read operations
        Task MarkAsReadAsync(Guid messageId, Guid profileId);
        Task<IEnumerable<MessageRead>> GetReadsForMessageAsync(Guid messageId);
        Task<MessageRead?> GetMessageReadAsync(Guid messageId, Guid profileId);
    }
}

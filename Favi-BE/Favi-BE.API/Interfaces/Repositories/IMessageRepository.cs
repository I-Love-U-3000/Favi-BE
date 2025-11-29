using Favi_BE.API.Models.Entities;
using Favi_BE.Interfaces.Repositories;

namespace Favi_BE.API.Interfaces.Repositories
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<(IEnumerable<Message> Items, int Total)> GetMessagesForConversationAsync(Guid conversationId, int skip, int take);

        Task<Message?> GetLastMessageAsync(Guid conversationId);
    }
}

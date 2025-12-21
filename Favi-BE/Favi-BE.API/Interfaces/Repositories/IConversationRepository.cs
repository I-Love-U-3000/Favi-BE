using Favi_BE.API.Models.Entities;
using Favi_BE.Interfaces.Repositories;

namespace Favi_BE.API.Interfaces.Repositories
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<Conversation?> FindDmConversationAsync(Guid profileA, Guid profileB);
        Task<IEnumerable<Conversation>> GetConversationsForUserAsync(Guid profileId, int skip, int take);
        Task<Conversation?> GetConversationWithMembersAsync(Guid conversationId);
    }
}

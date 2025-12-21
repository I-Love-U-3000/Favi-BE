using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Interfaces.Repositories;

namespace Favi_BE.API.Interfaces.Repositories
{
    public interface IUserConversationRepository : IGenericRepository<UserConversation>
    {
        Task<UserConversation?> GetAsync(Guid conversationId, Guid profileId);
        Task<IEnumerable<UserConversation>> GetMembersAsync(Guid conversationId);
    }
}

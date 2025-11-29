using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Data;
using Favi_BE.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Data.Repositories
{
    public class UserConversationRepository : GenericRepository<UserConversation>, IUserConversationRepository
    {
        public UserConversationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserConversation?> GetAsync(Guid conversationId, Guid profileId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(uc => uc.ConversationId == conversationId && uc.ProfileId == profileId);
        }

        public async Task<IEnumerable<UserConversation>> GetMembersAsync(Guid conversationId)
        {
            return await _dbSet
                .Where(uc => uc.ConversationId == conversationId)
                .Include(uc => uc.Profile)
                .ToListAsync();
        }
    }
}

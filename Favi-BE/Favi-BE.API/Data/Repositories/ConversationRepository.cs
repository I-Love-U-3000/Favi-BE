using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Enums;
using Favi_BE.Data;
using Favi_BE.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Data.Repositories
{
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Conversation?> FindDmConversationAsync(Guid profileA, Guid profileB)
        {
            // DM: đúng 2 thành viên A & B
            return await _dbSet
                .Where(c => c.Type == ConversationType.Dm)
                .Where(c =>
                    c.UserConversations.Count == 2 &&
                    c.UserConversations.Any(uc => uc.ProfileId == profileA) &&
                    c.UserConversations.Any(uc => uc.ProfileId == profileB))
                .Include(c => c.UserConversations)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Conversation>> GetConversationsForUserAsync(Guid profileId, int skip, int take)
        {
            return await _dbSet
                .Where(c => c.UserConversations.Any(uc => uc.ProfileId == profileId))
                .Include(c => c.UserConversations)
                    .ThenInclude(uc => uc.Profile)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Conversation?> GetConversationWithMembersAsync(Guid conversationId)
        {
            return await _dbSet
                .Where(c => c.Id == conversationId)
                .Include(c => c.UserConversations)
                    .ThenInclude(uc => uc.Profile)
                .FirstOrDefaultAsync();
        }
    }
}

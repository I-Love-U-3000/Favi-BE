using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Models.Entities;
using Favi_BE.Data;
using Favi_BE.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Data.Repositories
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Message> Items, int Total)> GetMessagesForConversationAsync(
            Guid conversationId, int skip, int take)
        {
            var query = _dbSet
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip(skip)
                .Take(take)
                .Include(m => m.Sender)
                .ToListAsync();

            // FE thường muốn ascending -> đảo lại
            items.Reverse();

            return (items, total);
        }

        public async Task<Message?> GetLastMessageAsync(Guid conversationId)
        {
            return await _dbSet
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid conversationId, DateTime afterTime)
        {
            return await _dbSet
                .Where(m => m.ConversationId == conversationId && m.CreatedAt > afterTime)
                .CountAsync();
        }
    }
}

using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Data;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Data.Repositories
{
    public class MessageReadRepository : IMessageReadRepository
    {
        private readonly AppDbContext _context;

        public MessageReadRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MessageRead?> GetAsync(Guid messageId, Guid profileId)
        {
            return await _context.MessageReads
                .FindAsync(new { messageId, profileId });
        }

        public async Task<IEnumerable<MessageRead>> GetReadsForMessageAsync(Guid messageId)
        {
            return await _context.MessageReads
                .Where(mr => mr.MessageId == messageId)
                .OrderBy(mr => mr.ReadAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid messageId, Guid profileId)
        {
            var existing = await GetAsync(messageId, profileId);
            if (existing != null)
            {
                // Already read, don't update
                return;
            }

            var messageRead = new MessageRead
            {
                MessageId = messageId,
                ProfileId = profileId,
                ReadAt = DateTime.UtcNow
            };

            await _context.MessageReads.AddAsync(messageRead);
        }

        public void Remove(MessageRead messageRead)
        {
            _context.MessageReads.Remove(messageRead);
        }
    }
}

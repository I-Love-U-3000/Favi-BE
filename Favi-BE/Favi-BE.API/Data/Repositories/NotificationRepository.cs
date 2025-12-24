using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.Data.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByRecipientIdAsync(Guid recipientId, int skip, int take)
        {
            return await _dbSet
                .Where(n => n.RecipientProfileId == recipientId)
                .Include(n => n.Actor)
                .Include(n => n.TargetPost)
                .Include(n => n.TargetComment)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid recipientId)
        {
            return await _dbSet
                .CountAsync(n => n.RecipientProfileId == recipientId && !n.IsRead);
        }
    }
}

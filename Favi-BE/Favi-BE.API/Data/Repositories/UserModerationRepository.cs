using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.Data.Repositories
{
    public class UserModerationRepository : GenericRepository<UserModeration>, IUserModerationRepository
    {
        public UserModerationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserModeration?> GetActiveModerationAsync(Guid profileId, ModerationActionType actionType)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(m => m.ProfileId == profileId &&
                            m.ActionType == actionType &&
                            m.Active &&
                            (!m.ExpiresAt.HasValue || m.ExpiresAt > now))
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<UserModeration>> GetByProfileAsync(Guid profileId, int skip, int take)
        {
            return await _dbSet
                .Where(m => m.ProfileId == profileId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}

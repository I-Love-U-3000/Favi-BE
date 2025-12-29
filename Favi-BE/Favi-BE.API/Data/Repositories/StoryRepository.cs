using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class StoryRepository : GenericRepository<Story>, IStoryRepository
    {
        public StoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Story>> GetActiveStoriesByProfileIdAsync(Guid profileId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(s => s.ProfileId == profileId
                    && s.ExpiresAt > now
                    && !s.IsArchived)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.Profile)
                .Include(s => s.StoryViews)
                .ToListAsync();
        }

        public async Task<Story?> GetActiveStoryWithDetailsAsync(Guid storyId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(s => s.Id == storyId)
                .Include(s => s.Profile)
                .Include(s => s.StoryViews)
                    .ThenInclude(sv => sv.Viewer)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Story>> GetViewableStoriesAsync(Guid viewerProfileId)
        {
            var now = DateTime.UtcNow;

            // Get profiles that viewer follows
            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == viewerProfileId)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            // Add viewer's own profile
            followingIds.Add(viewerProfileId);

            return await _dbSet
                .Where(s => followingIds.Contains(s.ProfileId)
                    && s.ExpiresAt > now
                    && !s.IsArchived)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.Profile)
                .Include(s => s.StoryViews)
                .ToListAsync();
        }

        public async Task<IEnumerable<Story>> GetStoriesByProfileIdAsync(Guid profileId, bool includeArchived = false)
        {
            var query = _dbSet.Where(s => s.ProfileId == profileId);

            if (!includeArchived)
                query = query.Where(s => !s.IsArchived);

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.Profile)
                .Include(s => s.StoryViews)
                .ToListAsync();
        }

        public async Task<IEnumerable<Story>> GetArchivedStoriesByProfileIdAsync(Guid profileId)
        {
            return await _dbSet
                .Where(s => s.ProfileId == profileId && s.IsArchived)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.Profile)
                .Include(s => s.StoryViews)
                .ToListAsync();
        }

        public async Task<int> CountActiveStoriesByProfileIdAsync(Guid profileId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .CountAsync(s => s.ProfileId == profileId
                    && s.ExpiresAt > now
                    && !s.IsArchived);
        }

        public async Task<IEnumerable<Story>> GetExpiredStoriesAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(s => s.ExpiresAt <= now && !s.IsArchived)
                .Include(s => s.Profile)
                .Include(s => s.StoryViews)
                .ToListAsync();
        }
    }
}

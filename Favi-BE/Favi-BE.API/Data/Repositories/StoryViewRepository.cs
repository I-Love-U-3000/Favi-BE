using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities.JoinTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class StoryViewRepository : GenericRepository<StoryView>, IStoryViewRepository
    {
        public StoryViewRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> HasViewedAsync(Guid storyId, Guid viewerProfileId)
        {
            return await _dbSet
                .AnyAsync(sv => sv.StoryId == storyId && sv.ViewerProfileId == viewerProfileId);
        }

        public async Task<IEnumerable<StoryView>> GetViewersByStoryIdAsync(Guid storyId)
        {
            return await _dbSet
                .Where(sv => sv.StoryId == storyId)
                .Include(sv => sv.Viewer)
                .OrderBy(sv => sv.ViewedAt)
                .ToListAsync();
        }

        public async Task<bool> RecordViewAsync(Guid storyId, Guid viewerProfileId)
        {
            // Check if already viewed
            if (await HasViewedAsync(storyId, viewerProfileId))
                return false;

            var storyView = new StoryView
            {
                StoryId = storyId,
                ViewerProfileId = viewerProfileId,
                ViewedAt = DateTime.UtcNow
            };

            await AddAsync(storyView);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> GetViewCountAsync(Guid storyId)
        {
            return await _dbSet
                .CountAsync(sv => sv.StoryId == storyId);
        }

        public async Task<IEnumerable<StoryView>> GetViewsByViewerAsync(Guid viewerProfileId)
        {
            return await _dbSet
                .Where(sv => sv.ViewerProfileId == viewerProfileId)
                .Include(sv => sv.Story)
                    .ThenInclude(s => s.Profile)
                .OrderByDescending(sv => sv.ViewedAt)
                .ToListAsync();
        }
    }
}

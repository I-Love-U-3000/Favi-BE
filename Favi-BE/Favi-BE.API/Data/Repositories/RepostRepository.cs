using Favi_BE.API.Models.Entities;
using Favi_BE.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.Data.Repositories
{
    public class RepostRepository : GenericRepository<Repost>, IRepostRepository
    {
        public RepostRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Repost?> GetRepostAsync(Guid profileId, Guid originalPostId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(r => r.ProfileId == profileId && r.OriginalPostId == originalPostId);
        }

        public async Task<IEnumerable<Repost>> GetRepostsByProfileAsync(Guid profileId, int skip, int take)
        {
            return await _dbSet
                .Where(r => r.ProfileId == profileId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(r => r.OriginalPost)
                .ThenInclude(p => p.Profile)
                .Include(r => r.OriginalPost)
                .ThenInclude(p => p.PostMedias)
                .Include(r => r.Profile)
                .Include(r => r.Comments)
                .Include(r => r.Reactions)
                .ToListAsync();
        }

        public async Task<int> GetRepostCountAsync(Guid postId)
        {
            return await _dbSet
                .CountAsync(r => r.OriginalPostId == postId);
        }

        public async Task<bool> HasRepostedAsync(Guid profileId, Guid postId)
        {
            return await _dbSet
                .AnyAsync(r => r.ProfileId == profileId && r.OriginalPostId == postId);
        }
    }
}

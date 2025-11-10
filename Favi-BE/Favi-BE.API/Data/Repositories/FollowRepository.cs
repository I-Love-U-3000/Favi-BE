using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities.JoinTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class FollowRepository : GenericRepository<Follow>, IFollowRepository
    {
        public FollowRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> IsFollowingAsync(Guid followerId, Guid followedId)
        {
            return await _dbSet.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followedId);
        }

        public async Task<IEnumerable<Follow>> GetFollowersAsync(Guid profileId, int skip, int take)
        {
            return await _dbSet
                .Where(f => f.FolloweeId == profileId)
                .Include(f => f.Follower)
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<Follow>> GetFollowingAsync(Guid profileId, int skip, int take)
        {
            return await _dbSet
                .Where(f => f.FollowerId == profileId)
                .Include(f => f.Followee)
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetFollowersCountAsync(Guid profileId)
        {
            return await _dbSet.CountAsync(f => f.FolloweeId == profileId);
        }

        public async Task<int> GetFollowingCountAsync(Guid profileId)
        {
            return await _dbSet.CountAsync(f => f.FollowerId == profileId);
        }

        public async Task<Follow?> GetAsync(Guid followerId, Guid followeeId)
        {
            return await _dbSet.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
        }
    }
}
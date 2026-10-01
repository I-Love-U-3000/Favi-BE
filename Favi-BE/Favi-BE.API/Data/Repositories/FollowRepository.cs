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

        public async Task<IEnumerable<Follow>> GetFollowersAsync(Guid profileId, int skip, int take, string? query = null)
        {
            var q = _dbSet
                .Where(f => f.FolloweeId == profileId)
                .Include(f => f.Follower)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var trimmed = query.Trim();
                q = q.Where(f => EF.Functions.ILike(f.Follower.Username, $"%{trimmed}%")
                              || (f.Follower.DisplayName != null && EF.Functions.ILike(f.Follower.DisplayName, $"%{trimmed}%")));
            }

            return await q
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<Follow>> GetFollowingAsync(Guid profileId, int skip, int take, string? query = null)
        {
            var q = _dbSet
                .Where(f => f.FollowerId == profileId)
                .Include(f => f.Followee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var trimmed = query.Trim();
                q = q.Where(f => EF.Functions.ILike(f.Followee.Username, $"%{trimmed}%")
                              || (f.Followee.DisplayName != null && EF.Functions.ILike(f.Followee.DisplayName, $"%{trimmed}%")));
            }

            return await q
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetFollowersCountAsync(Guid profileId, string? query = null)
        {
            var q = _dbSet.Where(f => f.FolloweeId == profileId);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var trimmed = query.Trim();
                q = q.Where(f => EF.Functions.ILike(f.Follower.Username, $"%{trimmed}%")
                              || (f.Follower.DisplayName != null && EF.Functions.ILike(f.Follower.DisplayName, $"%{trimmed}%")));
            }
            return await q.CountAsync();
        }

        public async Task<int> GetFollowingCountAsync(Guid profileId, string? query = null)
        {
            var q = _dbSet.Where(f => f.FollowerId == profileId);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var trimmed = query.Trim();
                q = q.Where(f => EF.Functions.ILike(f.Followee.Username, $"%{trimmed}%")
                              || (f.Followee.DisplayName != null && EF.Functions.ILike(f.Followee.DisplayName, $"%{trimmed}%")));
            }
            return await q.CountAsync();
        }

        public async Task<Follow?> GetAsync(Guid followerId, Guid followeeId)
        {
            return await _dbSet.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
        }

        public async Task<List<Guid>> GetFolloweeIdsAsync(Guid profileId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(f => f.FollowerId == profileId)
                .Select(f => f.FolloweeId)
                .ToListAsync(ct);
        }

        public async Task<List<Guid>> GetFollowerIdsAsync(Guid profileId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(f => f.FolloweeId == profileId)
                .Select(f => f.FollowerId)
                .ToListAsync(ct);
        }
    }
}
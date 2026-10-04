using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class CollectionRepository : GenericRepository<Collection>, ICollectionRepository
    {
        public CollectionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Collection>> GetCollectionsByProfileIdAsync(Guid profileId)
        {
            return await _dbSet
                .Where(c => c.ProfileId == profileId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Collection> GetCollectionWithPostsAsync(Guid collectionId)
        {
            return await _dbSet
                .Where(c => c.Id == collectionId)
                .Include(c => c.PostCollections)
                .ThenInclude(pc => pc.Post)
                .ThenInclude(p => p.PostMedias)
                .FirstOrDefaultAsync();
        }
        public async Task<(IEnumerable<Collection> Items, int Total)> GetAllByOwnerPagedAsync(Guid ownerId, int skip, int take)
        {
            var query = _dbSet
                .Where(c => c.ProfileId == ownerId)
                .Include(c => c.PostCollections)
                .OrderByDescending(c => c.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return (items, total);
        }

        public async Task<(IEnumerable<Collection> Items, int Total)> GetAllPagedAsync(int skip, int take)
        {
            var query = _dbSet
                .Include(c => c.PostCollections)
                .OrderByDescending(c => c.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return (items, total);
        }

        public async Task<List<Collection>> GetTrendingCandidatesAsync(int limit, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // 1. Fetch recently active public collections with posts
            var halfLimit = Math.Max(10, limit / 2);
            var candidates = await _dbSet
                .AsNoTracking()
                .AsSplitQuery()
                .Where(c => c.PrivacyLevel == Favi_BE.Models.Enums.PrivacyLevel.Public
                    && c.PostCollections.Any()
                    && (!c.Profile.IsBanned || (c.Profile.BannedUntil != null && c.Profile.BannedUntil <= now)))
                .Include(c => c.Profile)
                .Include(c => c.PostCollections)
                .Include(c => c.Reactions)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(halfLimit)
                .ToListAsync(ct);

            var candidateIds = candidates.Select(c => c.Id).ToHashSet();

            // 2. Fetch highest reacted public collections with posts that are not already included
            var needed = limit - candidates.Count;
            if (needed > 0)
            {
                var topCollectionIds = await _context.Reactions
                    .AsNoTracking()
                    .Where(r => r.CollectionId != null && !candidateIds.Contains(r.CollectionId.Value))
                    .GroupBy(r => r.CollectionId!.Value)
                    .Select(g => new { CollectionId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(needed)
                    .Select(x => x.CollectionId)
                    .ToListAsync(ct);

                if (topCollectionIds.Count > 0)
                {
                    var topEngaged = await _dbSet
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Where(c => topCollectionIds.Contains(c.Id)
                            && c.PrivacyLevel == Favi_BE.Models.Enums.PrivacyLevel.Public
                            && c.PostCollections.Any()
                            && (!c.Profile.IsBanned || (c.Profile.BannedUntil != null && c.Profile.BannedUntil <= now)))
                        .Include(c => c.Profile)
                        .Include(c => c.PostCollections)
                        .Include(c => c.Reactions)
                        .ToListAsync(ct);

                    candidates.AddRange(topEngaged);
                }
            }

            return candidates;
        }
    }
}
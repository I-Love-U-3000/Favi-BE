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
            var window = now.AddDays(-30);

            // Fetch candidate collections that are public, non-banned creator, with posts
            var candidates = await _dbSet
                .Where(c => c.PrivacyLevel == Favi_BE.Models.Enums.PrivacyLevel.Public
                    && c.PostCollections.Any()
                    && (!c.Profile.IsBanned || (c.Profile.BannedUntil != null && c.Profile.BannedUntil <= now))
                    && (c.CreatedAt >= window || c.UpdatedAt >= window))
                .Include(c => c.Profile)
                .Include(c => c.PostCollections)
                .Include(c => c.Reactions)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(limit)
                .ToListAsync(ct);

            // Fallback: if not enough recent candidates (e.g. cold start), take top public collections with posts
            if (candidates.Count < 10)
            {
                var candidateIds = candidates.Select(c => c.Id).ToHashSet();
                var fallback = await _dbSet
                    .Where(c => c.PrivacyLevel == Favi_BE.Models.Enums.PrivacyLevel.Public
                        && c.PostCollections.Any()
                        && (!c.Profile.IsBanned || (c.Profile.BannedUntil != null && c.Profile.BannedUntil <= now))
                        && !candidateIds.Contains(c.Id))
                    .Include(c => c.Profile)
                    .Include(c => c.PostCollections)
                    .Include(c => c.Reactions)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(limit - candidates.Count)
                    .ToListAsync(ct);

                candidates.AddRange(fallback);
            }

            return candidates;
        }
    }
}
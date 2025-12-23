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
    }
}
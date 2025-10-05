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
    }
}
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities.JoinTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class PostCollectionRepository : GenericRepository<PostCollection>, IPostCollectionRepository
    {
        public PostCollectionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> ExistsInCollectionAsync(Guid postId, Guid collectionId)
        {
            return await _dbSet.AnyAsync(pc => pc.PostId == postId && pc.CollectionId == collectionId);
        }

        public async Task RemoveFromCollectionAsync(Guid postId, Guid collectionId)
        {
            var postCollection = await _dbSet.FirstOrDefaultAsync(
                pc => pc.PostId == postId && pc.CollectionId == collectionId);
                
            if (postCollection != null)
            {
                Remove(postCollection);
            }
        }
    }
}
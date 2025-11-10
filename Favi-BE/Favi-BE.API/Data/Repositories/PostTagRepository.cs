using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities.JoinTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class PostTagRepository : GenericRepository<PostTag>, IPostTagRepository
    {
        public PostTagRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PostTag>> GetByPostIdAsync(Guid postId)
        {
            return await _dbSet
                .Where(pt => pt.PostId == postId)
                .Include(pt => pt.Tag)
                .ToListAsync();
        }

        public async Task<IEnumerable<PostTag>> GetByTagIdAsync(Guid tagId)
        {
            return await _dbSet
                .Where(pt => pt.TagId == tagId)
                .Include(pt => pt.Post)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid postId, Guid tagId)
        {
            return await _dbSet.AnyAsync(pt => pt.PostId == postId && pt.TagId == tagId);
        }

        public async Task AddTagToPostAsync(Guid postId, Guid tagId)
        {
            if (!await ExistsAsync(postId, tagId))
            {
                await AddAsync(new PostTag
                {
                    PostId = postId,
                    TagId = tagId
                });
            }
        }

        public async Task RemoveTagFromPostAsync(Guid postId, Guid tagId)
        {
            var postTag = await _dbSet.FirstOrDefaultAsync(pt => pt.PostId == postId && pt.TagId == tagId);
            if (postTag != null)
            {
                Remove(postTag);
            }
        }

        public async Task<IEnumerable<Guid>> GetPostIdsByTagNameAsync(string tagName)
        {
            return await _context.PostTags
                .Where(pt => pt.Tag.Name.ToLower() == tagName.ToLower())
                .Select(pt => pt.PostId)
                .ToListAsync();
        }
    }
}
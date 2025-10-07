using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Tag> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<Tag>> GetTagsByPostIdAsync(Guid postId)
        {
            return await _context.PostTags
                .Where(pt => pt.PostId == postId)
                .Select(pt => pt.Tag)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames)
        {
            var result = new List<Tag>();
            
            foreach (var name in tagNames)
            {
                var normalizedName = name.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(normalizedName))
                    continue;
                    
                var tag = await GetByNameAsync(normalizedName);
                
                if (tag == null)
                {
                    tag = new Tag { Id = Guid.NewGuid(), Name = normalizedName };
                    await AddAsync(tag);
                }
                
                result.Add(tag);
            }
            
            return result;
        }

        public async Task<(IEnumerable<Tag> Tags, int TotalCount)> GetAllPagedAsync(int skip, int take)
        {
            var totalCount = await _dbSet.CountAsync();
            var tags = await _dbSet
                .OrderBy(t => t.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return (tags, totalCount);
        }
    }
}
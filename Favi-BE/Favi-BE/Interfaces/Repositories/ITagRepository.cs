using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<Tag> GetByNameAsync(string name);
        Task<IEnumerable<Tag>> GetTagsByPostIdAsync(Guid postId);
        Task<IEnumerable<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames);
        Task<(IEnumerable<Tag> Tags, int TotalCount)> GetAllPagedAsync(int skip, int take);
        Task<IEnumerable<Tag>> GetTagsWithNoPostsAsync()

    }
}
using Favi_BE.Models.Entities.JoinTables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IPostTagRepository : IGenericRepository<PostTag>
    {
        Task<IEnumerable<PostTag>> GetByPostIdAsync(Guid postId);
        Task<IEnumerable<PostTag>> GetByTagIdAsync(Guid tagId);
        Task<bool> ExistsAsync(Guid postId, Guid tagId);
        Task AddTagToPostAsync(Guid postId, Guid tagId);
        Task RemoveTagFromPostAsync(Guid postId, Guid tagId);
        Task<IEnumerable<Guid>> GetPostIdsByTagNameAsync(string tagName);
    }
}
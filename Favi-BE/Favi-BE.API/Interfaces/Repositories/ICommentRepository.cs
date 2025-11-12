using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId);
        Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId);
        Task<List<Comment>> GetAllByPostAsync(Guid postId);
        Task<(IReadOnlyList<Comment> roots, int totalRoots)> GetRootCommentsPagedAsync(Guid postId, int page, int pageSize);
        Task<IReadOnlyList<Comment>> GetDirectRepliesForParentsAsync(IEnumerable<Guid> parentIds);
    }
}
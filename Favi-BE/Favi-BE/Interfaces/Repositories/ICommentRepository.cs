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
    }
}
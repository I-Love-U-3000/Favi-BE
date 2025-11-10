using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId)
        {
            return await _dbSet
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .Include(c => c.Profile)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId)
        {
            return await _dbSet
                .Where(c => c.ParentCommentId == parentCommentId)
                .Include(c => c.Profile)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
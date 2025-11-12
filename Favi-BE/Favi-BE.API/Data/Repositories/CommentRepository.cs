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
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .Include(c => c.Profile)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy TẤT CẢ comments của 1 post (cả root lẫn reply) — dùng cho Cách 2 (client tự group theo parent).
        /// </summary>
        public async Task<List<Comment>> GetAllByPostAsync(Guid postId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.PostId == postId)
                .Include(c => c.Profile)
                .OrderByDescending(c => c.CreatedAt) // hoặc OrderBy nếu muốn cũ trước
                .ToListAsync();
        }

        /// <summary>
        /// Lấy replies trực tiếp của một parent comment.
        /// </summary>
        public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.ParentCommentId == parentCommentId)
                .Include(c => c.Profile)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<Comment> roots, int totalRoots)> GetRootCommentsPagedAsync(Guid postId, int page, int pageSize)
        {
            var q = _dbSet.AsNoTracking().Where(c => c.PostId == postId && c.ParentCommentId == null);
            var total = await q.CountAsync();
            var roots = await q.Include(c => c.Profile)
                               .OrderByDescending(c => c.CreatedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
            return (roots, total);
        }

        public async Task<IReadOnlyList<Comment>> GetDirectRepliesForParentsAsync(IEnumerable<Guid> parentIds)
        {
            var ids = parentIds.Distinct().ToArray();
            if (ids.Length == 0) return Array.Empty<Comment>();
            return await _dbSet.AsNoTracking()
                .Where(c => c.ParentCommentId != null && ids.Contains(c.ParentCommentId.Value))
                .Include(c => c.Profile)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
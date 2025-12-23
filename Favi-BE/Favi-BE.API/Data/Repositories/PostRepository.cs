using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class PostRepository : GenericRepository<Post>, IPostRepository
    {
        public PostRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Post>> GetPostsByProfileIdAsync(Guid profileId, int skip, int take)
        {
            return await _dbSet
                .Where(p => p.ProfileId == profileId && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsWithMediaAsync(int skip, int take)
        {
            return await _dbSet
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        public async Task<Post?> GetPostWithDetailsAsync(Guid postId)
        {
            return await _dbSet
                .Where(p => p.Id == postId)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments.Where(c => c.ParentCommentId == null))
                .ThenInclude(c => c.Profile)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Post>> GetFeedByFollowingsAsync(Guid profileId, int skip, int take)
        {
            return await _dbSet
                .Where(p => _context.Follows.Any(f => f.FollowerId == profileId && f.FolloweeId == p.ProfileId) && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByTagIdAsync(Guid tagId, int skip, int take)
        {
            return await _dbSet
                .Where(p => p.PostTags.Any(pt => pt.TagId == tagId) && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByCollectionIdAsync(Guid collectionId, int skip, int take)
        {
            return await _dbSet
                .Where(p => p.PostCollections.Any(pc => pc.CollectionId == collectionId) && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetLatestPostsAsync(int skip, int take)
        {
            return await _dbSet
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        public async Task<Post?> GetPostWithAllAsync(Guid postId)
        {
            return await _dbSet
                .Where(p => p.Id == postId)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments).ThenInclude(c => c.Profile)
                .Include(p => p.Reactions).ThenInclude(r => r.Profile)
                .FirstOrDefaultAsync();
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetFeedPagedAsync(Guid profileId, int skip, int take)
        {
            var baseQuery = _dbSet
                .Where(p => (_context.Follows.Any(f => f.FollowerId == profileId && f.FolloweeId == p.ProfileId) || p.ProfileId == profileId) && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt);

            var total = await baseQuery.CountAsync();

            var items = await baseQuery
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Skip(skip).Take(take)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetPostsByTagPagedAsync(Guid tagId, int skip, int take)
        {
            var query = _dbSet
                .Where(p => p.PostTags.Any(pt => pt.TagId == tagId) && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                // 👇 Thêm include Reaction (và Comment nếu cần)
                .Include(p => p.Reactions)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }


        public async Task<(IEnumerable<Post> Items, int Total)> GetPostsByCollectionPagedAsync(Guid collectionId, int skip, int take)
        {
            var query = _dbSet
                .Where(p => p.PostCollections.Any(pc => pc.CollectionId == collectionId) && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                // 👇 Thêm Reactions để service map ReactionSummaryDto đầy đủ
                .Include(p => p.Reactions)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }
    }
}
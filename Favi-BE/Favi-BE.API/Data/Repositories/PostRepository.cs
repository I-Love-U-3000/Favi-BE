using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
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
            Console.WriteLine($"[PostRepository] GetPostsByProfileIdAsync called with profileId: {profileId}, skip: {skip}, take: {take}");
            
            var result = await _dbSet
                .Where(p => p.ProfileId == profileId && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Reactions)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            
            Console.WriteLine($"[PostRepository] Found {result.Count} posts for profileId: {profileId}");
            foreach (var post in result)
            {
                Console.WriteLine($"[PostRepository]   - Post {post.Id} belongs to ProfileId: {post.ProfileId}");
            }
            
            return result;
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
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(p => _context.Follows.Any(f => f.FollowerId == profileId && f.FolloweeId == p.ProfileId)
                    && p.Privacy != PrivacyLevel.Private
                    && (!p.Profile.IsBanned || (p.Profile.BannedUntil != null && p.Profile.BannedUntil <= now))
                    && p.DeletedDayExpiredAt == null && !p.IsArchived)
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
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived
                    && p.Privacy == PrivacyLevel.Public
                    && (!p.Profile.IsBanned || (p.Profile.BannedUntil != null && p.Profile.BannedUntil <= now)))
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
            var now = DateTime.UtcNow;
            var baseQuery = _dbSet
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived &&
                    (
                        p.ProfileId == profileId
                        ||
                        (
                            _context.Follows.Any(f => f.FollowerId == profileId && f.FolloweeId == p.ProfileId)
                            && p.Privacy != PrivacyLevel.Private
                            && (!p.Profile.IsBanned || (p.Profile.BannedUntil != null && p.Profile.BannedUntil <= now))
                        )
                    )
                )
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

        public async Task<IEnumerable<Post>> GetPostsByTagIdsPagedAsync(List<Guid> tagIds, int page, int pageSize)
        {
            if (!tagIds.Any())
                return Enumerable.Empty<Post>();

            var skip = (page - 1) * pageSize;

            return await _dbSet
                .Where(p => p.PostTags.Any(pt => tagIds.Contains(pt.TagId)) && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> SearchPostsByCaptionAsync(string query, int skip, int take)
        {
            return await _dbSet
                .Where(p => p.Caption != null && p.Caption.ToLower().Contains(query.ToLower()))
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived)
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetProfilePostsPagedAsync(
            Guid profileId, int skip, int take, Guid? viewerId = null)
        {
            var isOwner = viewerId.HasValue && viewerId.Value == profileId;
            var isAdmin = viewerId.HasValue && await _context.Profiles.AnyAsync(p => p.Id == viewerId.Value && p.Role == UserRole.Admin);
            var isFollowing = viewerId.HasValue && !isOwner && !isAdmin &&
                await _context.Follows.AnyAsync(f => f.FollowerId == viewerId.Value && f.FolloweeId == profileId);

            var query = _dbSet
                .Where(p => p.ProfileId == profileId && p.DeletedDayExpiredAt == null && !p.IsArchived)
                .Where(p => isOwner || isAdmin
                    || (isFollowing && (p.Privacy == PrivacyLevel.Public || p.Privacy == PrivacyLevel.Followers))
                    || (!isFollowing && p.Privacy == PrivacyLevel.Public))
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Skip(skip).Take(take)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetLatestPostsPagedAsync(int skip, int take)
        {
            var now = DateTime.UtcNow;
            var query = _dbSet
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived
                    && p.Privacy == PrivacyLevel.Public
                    && (!p.Profile.IsBanned || (p.Profile.BannedUntil != null && p.Profile.BannedUntil <= now)))
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Skip(skip).Take(take)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetExploreFeedPagedAsync(
            Guid profileId, int skip, int take)
        {
            var now = DateTime.UtcNow;
            // Explore: posts not from self and not from followings, public only
            var followeeIds = await _context.Follows
                .Where(f => f.FollowerId == profileId)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            var query = _dbSet
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived
                    && p.ProfileId != profileId
                    && !followeeIds.Contains(p.ProfileId)
                    && p.Privacy == PrivacyLevel.Public
                    && (!p.Profile.IsBanned || (p.Profile.BannedUntil != null && p.Profile.BannedUntil <= now)))
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Skip(skip).Take(take)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetGuestFeedPagedAsync(int skip, int take)
        {
            var now = DateTime.UtcNow;
            var query = _dbSet
                .Where(p => p.DeletedDayExpiredAt == null && !p.IsArchived
                    && p.Privacy == PrivacyLevel.Public
                    && (!p.Profile.IsBanned || (p.Profile.BannedUntil != null && p.Profile.BannedUntil <= now)))
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.Comments)
                .Skip(skip).Take(take)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetArchivedByProfilePagedAsync(
            Guid profileId, int skip, int take)
        {
            var query = _dbSet
                .Where(p => p.ProfileId == profileId && p.IsArchived && p.DeletedDayExpiredAt == null)
                .OrderByDescending(p => p.UpdatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Skip(skip).Take(take)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IEnumerable<Post> Items, int Total)> GetRecycleBinByProfilePagedAsync(
            Guid profileId, int skip, int take)
        {
            var query = _dbSet
                .Where(p => p.ProfileId == profileId && p.DeletedDayExpiredAt != null)
                .OrderByDescending(p => p.DeletedDayExpiredAt);

            var total = await query.CountAsync();
            var items = await query
                .Include(p => p.Profile)
                .Include(p => p.PostMedias)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Skip(skip).Take(take)
                .ToListAsync();

            return (items, total);
        }
    }
}
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICloudinaryService _cloudinary;
        private readonly IPrivacyGuard _privacy;
        private readonly IVectorIndexService _vectorIndex;
        private readonly INSFWService _nsfwService;
        private readonly INotificationService? _notificationService;
        private readonly IAuditService? _auditService;

        // ------- Trending Score constants -------
        private const double W_Like = 1.0;    // Wl
        private const double W_Comment = 3.0; // Wc
        private const double W_Share = 5.0;   // Ws (tạm 0 ở hiện tại)
        private const double W_View = 0.2;    // Wv (tạm 0 ở hiện tại)

        private const double Lambda = 0.1;            // e^{-λ * Δt}, Δt tính theo giờ
        private const double Beta = 0.5;              // hệ số khuếch đại velocity
        private const double MaxAgeHours = 720;        // chỉ xét bài trong ~3 ngày
        private static readonly TimeSpan VelocityWindow = TimeSpan.FromHours(1);
        private const int TrendingCandidateLimit = 500; // tối đa ứng viên để tính trending

        public PostService(IUnitOfWork uow, ICloudinaryService cloudinary, IPrivacyGuard privacy, IVectorIndexService vectorIndex, INSFWService nsfwService, INotificationService? notificationService = null, IAuditService? auditService = null)
        {
            _uow = uow;
            _cloudinary = cloudinary;
            _privacy = privacy;
            _vectorIndex = vectorIndex;
            _nsfwService = nsfwService;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public Task<Post?> GetEntityAsync(Guid id) => _uow.Posts.GetByIdAsync(id);

        // --------------------------------------------------------------------
        // Queries
        // --------------------------------------------------------------------
        public async Task<PostResponse?> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var post = await _uow.Posts.GetPostWithAllAsync(id);
            if (post is null) return null;

            return await MapPostToResponseAsync(post, currentUserId);
        }

        /// <summary>
        /// Feed cho user đã đăng nhập:
        /// - Lấy pool bài viết từ bản thân + followees (repo GetFeedPagedAsync).
        /// - Áp dụng TrendingScore(p, t) = (W.L + Wc.C + Ws.S + Wv.V) * e^{-λΔt} * (1 + β * ΔE/Δt).
        /// - Chỉ xét bài trong 72h, có quyền xem, sort theo score, rồi phân trang.
        /// </summary>
        public async Task<PagedResult<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;

            // Lấy candidate từ repo: bài của chính user + người user follow,
            // giới hạn tối đa TrendingCandidateLimit mới nhất.
            var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
                currentUserId,
                skip: 0,
                take: TrendingCandidateLimit
            );

            var now = DateTime.UtcNow;
            var scored = new List<(Post Post, double Score)>();

            foreach (var post in candidates)
            {
                // Check quyền xem theo privacy
                if (!await _privacy.CanViewPostAsync(post, currentUserId))
                    continue;

                // Chỉ xét bài trong khoảng thời gian cho phép
                var ageHours = (now - post.CreatedAt).TotalHours;
                if (ageHours > MaxAgeHours) continue;

                var score = await ComputeTrendingScoreAsync(post);

                // Nhẹ nhàng boost nếu là bài của chính user
                if (post.ProfileId == currentUserId)
                    score *= 1.1;

                scored.Add((post, score));
            }

            var ordered = scored
                .OrderByDescending(x => x.Score)
                .ToList();

            var total = ordered.Count;

            var pageItems = ordered
                .Skip(skip)
                .Take(pageSize)
                .Select(x => x.Post)
                .ToList();

            var responses = new List<PostResponse>();
            foreach (var p in pageItems)
                responses.Add(await MapPostToResponseAsync(p, currentUserId));

            return new PagedResult<PostResponse>(responses, page, pageSize, total);
        }

        /// <summary>
        /// Feed cho guest (chưa đăng nhập):
        /// - Lấy các bài mới nhất toàn hệ thống (pool max 500).
        /// - Chỉ lấy bài mà guest có quyền xem (thường là Public).
        /// - Áp dụng TrendingScore và sort giảm dần.
        /// </summary>
        public async Task<PagedResult<PostResponse>> GetGuestFeedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;

            var latestCandidates = await _uow.Posts.GetLatestPostsAsync(
                skip: 0,
                take: TrendingCandidateLimit
            );

            var now = DateTime.UtcNow;
            var scored = new List<(Post Post, double Score)>();

            foreach (var post in latestCandidates)
            {
                if (!await _privacy.CanViewPostAsync(post, null))
                    continue;

                var ageHours = (now - post.CreatedAt).TotalHours;
                if (ageHours > MaxAgeHours) continue;

                var score = await ComputeTrendingScoreAsync(post);

                scored.Add((post, score));
            }

            var ordered = scored
                .OrderByDescending(x => x.Score)
                .ToList();

            var total = ordered.Count;

            var pageItems = ordered
                .Skip(skip)
                .Take(pageSize)
                .Select(x => x.Post)
                .ToList();

            var responses = new List<PostResponse>();
            foreach (var p in pageItems)
                responses.Add(await MapPostToResponseAsync(p, null));

            return new PagedResult<PostResponse>(responses, page, pageSize, total);
        }

        // --------------------------------------------------------------------
        // Commands: Create/Update/Delete
        // --------------------------------------------------------------------
        public async Task<PostResponse> CreateAsync(
            Guid authorId,
            string? caption,
            IEnumerable<string>? tags,
            PrivacyLevel privacyLevel,
            LocationDto? location)
        {
            // Call the overload without media files for backward compatibility
            return await CreateAsync(authorId, caption, tags, privacyLevel, location, null);
        }

        public async Task<PostResponse> CreateAsync(
            Guid authorId,
            string? caption,
            IEnumerable<string>? tags,
            PrivacyLevel privacyLevel,
            LocationDto? location,
            List<IFormFile>? mediaFiles)
        {
            var now = DateTime.UtcNow;
            var post = new Post
            {
                Id = Guid.NewGuid(),
                ProfileId = authorId,
                Caption = caption?.Trim(),
                Privacy = privacyLevel,
                CreatedAt = now,
                UpdatedAt = now,
                LocationName = location?.Name,
                LocationFullAddress = location?.FullAddress,
                LocationLatitude = location?.Latitude,
                LocationLongitude = location?.Longitude
            };

            await _uow.Posts.AddAsync(post);

            if (tags != null && tags.Any())
            {
                var createdTags = await _uow.Tags.GetOrCreateTagsAsync(tags);
                foreach (var t in createdTags)
                {
                    await _uow.PostTags.AddTagToPostAsync(post.Id, t.Id);
                }
            }

            // Handle media files if provided
            List<PostMedia>? createdMedias = null;
            if (mediaFiles != null && mediaFiles.Any())
            {
                // Filter valid media files
                var allowedFiles = mediaFiles
                    .Where(f => f?.Length > 0
                             && !string.IsNullOrWhiteSpace(f.ContentType)
                             && f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (allowedFiles.Count > 0)
                {
                    createdMedias = new List<PostMedia>();
                    var startPosition = 0;

                    foreach (var file in allowedFiles)
                    {
                        var uploaded = await _cloudinary.TryUploadAsync(file);
                        if (uploaded == null)
                        {
                            // If any media upload fails, rollback the entire transaction
                            throw new InvalidOperationException($"Failed to upload media file: {file.FileName}. Post creation cancelled.");
                        }

                        var media = new PostMedia
                        {
                            Id = Guid.NewGuid(),
                            PostId = post.Id,
                            Url = uploaded.Url,
                            ThumbnailUrl = uploaded.ThumbnailUrl,
                            Position = startPosition++,
                            PublicId = uploaded.PublicId,
                            Width = uploaded.Width,
                            Height = uploaded.Height,
                            Format = uploaded.Format
                        };

                        createdMedias.Add(media);
                        await _uow.PostMedia.AddAsync(media);
                    }
                }
            }

            try
            {
                await _uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create post. The post could not be saved to the database.", ex);
            }

            // Vectorize post for semantic search (fire-and-forget, don't block post creation)
            var full = await _uow.Posts.GetPostWithAllAsync(post.Id);
            if (full != null && _vectorIndex.IsEnabled())
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _vectorIndex.IndexPostAsync(full);
                    }
                    catch
                    {
                        // Swallow - errors logged in VectorIndexService
                    }
                });
            }

            // Check NSFW content
            if (full != null && _nsfwService.IsEnabled())
            {
                try
                {
                    full.IsNSFW = await _nsfwService.CheckPostAsync(full);
                    _uow.Posts.Update(full);
                    await _uow.CompleteAsync();
                }
                catch
                {
                    // Swallow - errors logged in NSFWService
                }
            }

            return await MapPostToResponseAsync(full ?? post, authorId);
        }

        public async Task<bool> UpdateAsync(Guid postId, Guid requesterId, string? caption)
        {
            var post = await _uow.Posts.GetPostWithAllAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            if (caption != null)
                post.Caption = caption.Trim();

            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);
            await _uow.CompleteAsync();

            // Re-check NSFW content when caption is updated
            if (_nsfwService.IsEnabled())
            {
                try
                {
                    post.IsNSFW = await _nsfwService.CheckPostAsync(post);
                    _uow.Posts.Update(post);
                    await _uow.CompleteAsync();
                }
                catch
                {
                    // Swallow - errors logged in NSFWService
                }
            }

            return true;
        }

        public async Task<bool> DeleteAsync(Guid postId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            // Soft delete: set expiration date to 30 days from now
            post.DeletedDayExpiredAt = DateTime.UtcNow.AddDays(30);
            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<bool> RestoreAsync(Guid postId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;
            if (post.DeletedDayExpiredAt is null) return false; // Not deleted

            // Restore from recycle bin
            post.DeletedDayExpiredAt = null;
            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<bool> PermanentDeleteAsync(Guid postId, Guid requesterId)
        {
            var post = await _uow.Posts.GetPostWithAllAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            // Delete media from Cloudinary
            if (post.PostMedias != null)
            {
                foreach (var media in post.PostMedias)
                {
                    if (!string.IsNullOrEmpty(media.PublicId))
                    {
                        _ = _cloudinary.TryDeleteAsync(media.PublicId);
                    }
                }
            }

            // Remove from vector index
            if (_vectorIndex.IsEnabled())
            {
                try
                {
                    // Note: VectorIndexService doesn't currently support RemovePostAsync
                    // The post will remain in the index but won't appear in search results if marked as deleted
                }
                catch
                {
                    // Swallow - errors logged in VectorIndexService
                }
            }

            // Hard delete the post
            _uow.Posts.Remove(post);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<bool> ArchiveAsync(Guid postId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;
            if (post.DeletedDayExpiredAt is not null) return false; // Cannot archive deleted post

            post.IsArchived = true;
            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<bool> UnarchiveAsync(Guid postId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            post.IsArchived = false;
            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<PagedResult<PostResponse>> GetRecycleBinAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;

            // Get all soft-deleted posts for this user
            var allDeletedPosts = await _uow.Posts.FindAsync(p =>
                p.ProfileId == userId && p.DeletedDayExpiredAt != null);

            var deletedPosts = allDeletedPosts
                .OrderByDescending(p => p.DeletedDayExpiredAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var responses = new List<PostResponse>();
            foreach (var p in deletedPosts)
                responses.Add(await MapPostToResponseAsync(p, userId));

            return new PagedResult<PostResponse>(responses, page, pageSize, allDeletedPosts.Count());
        }

        public async Task<PagedResult<PostResponse>> GetArchivedAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;

            // Get all archived posts for this user
            var allArchivedPosts = await _uow.Posts.FindAsync(p =>
                p.ProfileId == userId && p.IsArchived && p.DeletedDayExpiredAt == null);

            var archivedPosts = allArchivedPosts
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var responses = new List<PostResponse>();
            foreach (var p in archivedPosts)
                responses.Add(await MapPostToResponseAsync(p, userId));

            return new PagedResult<PostResponse>(responses, page, pageSize, allArchivedPosts.Count());
        }

        // --------------------------------------------------------------------
        // Media
        // --------------------------------------------------------------------
        public async Task<IEnumerable<PostMediaResponse>> UploadMediaAsync(
            Guid postId,
            IEnumerable<IFormFile> files,
            Guid requesterId)
        {
            var post = await _uow.Posts.GetPostWithAllAsync(postId);
            if (post is null || post.ProfileId != requesterId)
                return Enumerable.Empty<PostMediaResponse>();

            var fileList = (files ?? Enumerable.Empty<IFormFile>()).ToList();
            if (fileList.Count == 0) return Enumerable.Empty<PostMediaResponse>();

            var allowed = fileList
                .Where(f => f?.Length > 0
                         && !string.IsNullOrWhiteSpace(f.ContentType)
                         && f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (allowed.Count == 0) return Enumerable.Empty<PostMediaResponse>();

            var existing = await _uow.PostMedia.GetByPostIdAsync(postId);
            var maxPos = existing.Any() ? existing.Max(m => m.Position) : -1;
            var startPos = maxPos + 1;

            var createdMedias = new List<PostMedia>();
            var responses = new List<PostMediaResponse>();

            for (int i = 0; i < allowed.Count; i++)
            {
                var file = allowed[i];

                var uploaded = await _cloudinary.TryUploadAsync(file);
                if (uploaded is null) continue;

                var media = new PostMedia
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    Url = uploaded.Url,
                    ThumbnailUrl = uploaded.ThumbnailUrl,
                    Position = startPos + i,
                    PublicId = uploaded.PublicId,
                    Width = uploaded.Width,
                    Height = uploaded.Height,
                    Format = uploaded.Format
                };

                createdMedias.Add(media);

                responses.Add(new PostMediaResponse(
                    media.Id, media.PostId ?? Guid.Empty, media.Url,
                    media.PublicId, media.Width, media.Height,
                    media.Format, media.Position, media.ThumbnailUrl
                ));
            }

            if (createdMedias.Count == 0)
                return Enumerable.Empty<PostMediaResponse>();

            await _uow.PostMedia.AddRangeAsync(createdMedias);
            post.UpdatedAt = DateTime.UtcNow;
            _uow.Posts.Update(post);
            await _uow.CompleteAsync();

            // Re-check NSFW content when media is uploaded
            if (_nsfwService.IsEnabled())
            {
                try
                {
                    // Reload post with new media
                    var updatedPost = await _uow.Posts.GetPostWithAllAsync(postId);
                    if (updatedPost != null)
                    {
                        updatedPost.IsNSFW = await _nsfwService.CheckPostAsync(updatedPost);
                        _uow.Posts.Update(updatedPost);
                        await _uow.CompleteAsync();
                    }
                }
                catch
                {
                    // Swallow - errors logged in NSFWService
                }
            }

            return responses.OrderBy(r => r.Position);
        }

        public async Task<bool> RemoveMediaAsync(Guid postId, Guid mediaId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            var medias = await _uow.PostMedia.GetByPostIdAsync(postId);
            var media = medias.FirstOrDefault(m => m.Id == mediaId);
            if (media is null) return false;

            _uow.PostMedia.Remove(media);

            var remain = medias.Where(m => m.Id != mediaId).OrderBy(m => m.Position).ToList();
            for (int i = 0; i < remain.Count; i++)
            {
                if (remain[i].Position != i)
                {
                    remain[i].Position = i;
                    _uow.PostMedia.Update(remain[i]);
                }
            }

            await _uow.CompleteAsync();
            return true;
        }

        // --------------------------------------------------------------------
        // Tags
        // --------------------------------------------------------------------
        public async Task<IEnumerable<TagDto>> AddTagsAsync(Guid postId, IEnumerable<string> tags, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return Enumerable.Empty<TagDto>();
            if (post.ProfileId != requesterId) return Enumerable.Empty<TagDto>();

            var created = await _uow.Tags.GetOrCreateTagsAsync(tags);
            foreach (var t in created)
                await _uow.PostTags.AddTagToPostAsync(postId, t.Id);

            await _uow.CompleteAsync();

            var result = await _uow.Tags.GetTagsByPostIdAsync(postId);
            return result.Select(t => new TagDto(t.Id, t.Name));
        }

        public async Task<bool> RemoveTagAsync(Guid postId, Guid tagId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            await _uow.PostTags.RemoveTagFromPostAsync(postId, tagId);
            await _uow.CompleteAsync();
            return true;
        }

        // --------------------------------------------------------------------
        // Reactions
        // --------------------------------------------------------------------
        public async Task<ReactionSummaryDto> GetReactionsAsync(Guid postId, Guid? currentUserId)
        {
            return await BuildReactionSummaryAsync(postId, currentUserId);
        }

        public async Task<ReactionType?> ToggleReactionAsync(Guid postId, Guid userId, ReactionType type)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return null;

            var existing = await _uow.Reactions.GetProfileReactionOnPostAsync(userId, postId);

            if (existing is null)
            {
                await _uow.Reactions.AddAsync(new Reaction
                {
                    PostId = postId,
                    ProfileId = userId,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                });
                await _uow.CompleteAsync();

                await _notificationService.CreatePostReactionNotificationAsync(userId, postId);

                return type;
            }

            if (existing.Type == type)
            {
                _uow.Reactions.Remove(existing);
                await _uow.CompleteAsync();
                return null;
            }

            existing.Type = type;
            _uow.Reactions.Update(existing);
            await _uow.CompleteAsync();
            return type;
        }

        public async Task<IEnumerable<PostReactorResponse>> GetReactorsAsync(Guid postId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found");

            // Check if user can view this post's reactors using privacy guard
            var canView = await _privacy.CanViewPostAsync(post, requesterId);
            if (!canView)
                throw new UnauthorizedAccessException("You don't have permission to view reactors for this post");

            var reactions = await _uow.Reactions.GetReactionsByPostIdAsync(postId);

            return reactions.Select(r => new PostReactorResponse(
                r.Profile.Id,
                r.Profile.Username,
                r.Profile.DisplayName,
                r.Profile.AvatarUrl,
                r.Type,
                r.CreatedAt
            ));
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------
        private async Task<PostResponse> MapPostToResponseAsync(Post post, Guid? currentUserId)
        {
            if (post.PostMedias is null || post.PostTags is null || post.Comments is null || post.Reactions is null)
                post = await _uow.Posts.GetPostWithAllAsync(post.Id) ?? post;

            var medias = (post.PostMedias ?? new List<PostMedia>())
                .OrderBy(m => m.Position)
                .Select(m => new PostMediaResponse(
                    m.Id,
                    m.PostId ?? Guid.Empty,
                    m.Url,
                    PublicId: string.Empty,   // nếu muốn có PublicId thật thì map từ entity
                    Width: 0,
                    Height: 0,
                    Format: string.Empty,
                    Position: m.Position,
                    ThumbnailUrl: m.ThumbnailUrl
                ));

            var tags = (post.PostTags ?? new List<PostTag>())
                .Select(pt => new TagDto(pt.Tag.Id, pt.Tag.Name));

            var summary = await BuildReactionSummaryAsync(post.Id, currentUserId);

            var location = (post.LocationName != null ||
                            post.LocationFullAddress != null ||
                            post.LocationLatitude != null ||
                            post.LocationLongitude != null)
                ? new LocationDto(
                    post.LocationName,
                    post.LocationFullAddress,
                    post.LocationLatitude,
                    post.LocationLongitude
                  )
                : null;

            return new PostResponse(
                Id: post.Id,
                AuthorProfileId: post.ProfileId,
                Caption: post.Caption,
                CreatedAt: post.CreatedAt,
                UpdatedAt: post.UpdatedAt,
                PrivacyLevel: post.Privacy,
                Medias: medias,
                Tags: tags,
                Reactions: summary,
                CommentsCount: post.Comments?.Count ?? 0,
                Location: location,
                IsNSFW: post.IsNSFW
            );
        }

        private async Task<ReactionSummaryDto> BuildReactionSummaryAsync(Guid postId, Guid? currentUserId)
        {
            var reactions = await _uow.Reactions.GetReactionsByPostIdAsync(postId);

            var byType = reactions
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            var total = byType.Values.Sum();

            ReactionType? mine = null;
            if (currentUserId.HasValue)
            {
                var my = reactions.FirstOrDefault(r => r.ProfileId == currentUserId.Value);
                if (my != null) mine = my.Type;
            }

            return new ReactionSummaryDto(total, byType, mine);
        }

        public async Task<PagedResult<PostResponse>> GetByProfileAsync(Guid profileId, Guid? viewerId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var posts = await _uow.Posts.GetPostsByProfileIdAsync(profileId, skip, pageSize);
            var total = await _uow.Posts.CountAsync(p => p.ProfileId == profileId);

            var result = new List<PostResponse>();
            foreach (var post in posts)
            {
                if (await _privacy.CanViewPostAsync(post, viewerId))
                    result.Add(await MapPostToResponseAsync(post, viewerId));
            }

            return new PagedResult<PostResponse>(result, page, pageSize, total);
        }

        /// <summary>
        /// Tính TrendingScore(p, t) theo công thức trong hình.
        /// </summary>
        private async Task<double> ComputeTrendingScoreAsync(Post post)
        {
            var now = DateTime.UtcNow;

            var ageHours = Math.Max((now - post.CreatedAt).TotalHours, 0);
            if (ageHours > MaxAgeHours)
                return 0;

            var reactions = await _uow.Reactions.GetReactionsByPostIdAsync(post.Id);
            var likeCount = reactions.Count();

            var commentCount = post.Comments?.Count ?? 0;

            // Chưa track share/view thực → tạm 0
            var shareCount = 0;
            var viewCount = 0;

            // ✅ Base = 1 để post mới, chưa có interaction vẫn có điểm
            var engagement =
                1.0 +
                W_Like * likeCount +
                W_Comment * commentCount +
                W_Share * shareCount +
                W_View * viewCount;

            var decay = Math.Exp(-Lambda * ageHours);

            var from = now - VelocityWindow;
            var recentReactions = reactions.Count(r => r.CreatedAt >= from);
            var recentComments = (post.Comments ?? new List<Comment>())
                .Count(c => c.CreatedAt >= from);

            var deltaE = recentReactions + recentComments;
            var velocity = VelocityWindow.TotalHours > 0
                ? deltaE / VelocityWindow.TotalHours
                : 0;

            var score = engagement * decay * (1 + Beta * velocity);
            return score;
        }

        // --------------------------------------------------------------------
        // Explore & Latest (TODOs)
        // --------------------------------------------------------------------

        /// <summary>
        /// Explore: global trending cho user đã đăng nhập.
        /// - Lấy pool bài mới nhất toàn hệ thống.
        /// - Lọc theo quyền xem của user.
        /// - Tính TrendingScore và sort giảm dần.
        /// </summary>
        public async Task<PagedResult<PostResponse>> GetExploreAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;

            var latestCandidates = await _uow.Posts.GetLatestPostsAsync(
                skip: 0,
                take: TrendingCandidateLimit
            );

            var now = DateTime.UtcNow;
            var scored = new List<(Post Post, double Score)>();

            foreach (var post in latestCandidates)
            {
                if (!await _privacy.CanViewPostAsync(post, userId))
                    continue;

                var ageHours = (now - post.CreatedAt).TotalHours;
                if (ageHours > MaxAgeHours) continue;

                var score = await ComputeTrendingScoreAsync(post);

                scored.Add((post, score));
            }

            var ordered = scored
                .OrderByDescending(x => x.Score)
                .ToList();

            var total = ordered.Count;

            var pageItems = ordered
                .Skip(skip)
                .Take(pageSize)
                .Select(x => x.Post)
                .ToList();

            var responses = new List<PostResponse>();
            foreach (var p in pageItems)
                responses.Add(await MapPostToResponseAsync(p, userId));

            return new PagedResult<PostResponse>(responses, page, pageSize, total);
        }

        /// <summary>
        /// Latest: bài mới nhất toàn hệ thống (ưu tiên an toàn → chỉ bài Public).
        /// </summary>
        public async Task<PagedResult<PostResponse>> GetLatestAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;

            // Lấy pool mới nhất, sau đó lọc quyền xem như guest (viewerId = null).
            var candidates = await _uow.Posts.GetLatestPostsAsync(
                skip: 0,
                take: TrendingCandidateLimit
            );

            var visible = new List<Post>();
            foreach (var post in candidates)
            {
                if (await _privacy.CanViewPostAsync(post, null))
                    visible.Add(post);
            }

            var ordered = visible
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var total = ordered.Count;

            var pageItems = ordered
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var responses = new List<PostResponse>();
            foreach (var p in pageItems)
                responses.Add(await MapPostToResponseAsync(p, null));

            return new PagedResult<PostResponse>(responses, page, pageSize, total);
        }

        // --------------------------------------------------------------------
        // Admin Operations
        // --------------------------------------------------------------------
        public async Task<bool> AdminDeleteAsync(Guid postId, Guid adminId, string reason)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;

            // Soft delete: set expiration date to 30 days from now
            post.DeletedDayExpiredAt = DateTime.UtcNow.AddDays(30);
            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);

            // Log admin action
            if (_auditService != null)
            {
                await _auditService.LogAsync(new Favi_BE.Models.Entities.AdminAction
                {
                    Id = Guid.NewGuid(),
                    AdminId = adminId,
                    ActionType = Models.Enums.AdminActionType.DeleteContent,
                    TargetEntityId = postId,
                    TargetEntityType = "Post",
                    TargetProfileId = post.ProfileId,
                    Notes = reason,
                    CreatedAt = DateTime.UtcNow
                }, saveChanges: false);
            }

            await _uow.CompleteAsync();
            return true;
        }
    }
}
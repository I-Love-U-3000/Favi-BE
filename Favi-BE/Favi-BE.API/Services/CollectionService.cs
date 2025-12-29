using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Favi_BE.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICloudinaryService _cloudinary;

        public CollectionService(IUnitOfWork uow, ICloudinaryService cloudinary)
        {
            _uow = uow;
            _cloudinary = cloudinary;
        }

        public async Task<CollectionResponse> CreateAsync(Guid ownerId, CreateCollectionRequest dto, IFormFile? coverImage)
        {
            string? coverImageUrl = null;
            string? coverImagePublicId = null;

            // Handle cover image upload if provided
            if (coverImage != null && coverImage.Length > 0)
            {
                // Validate file type
                if (!coverImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Cover image must be an image file.");
                }

                var uploaded = await _cloudinary.TryUploadAsync(coverImage, folder: "favi_collections");
                if (uploaded == null)
                {
                    throw new InvalidOperationException($"Failed to upload cover image: {coverImage.FileName}");
                }

                coverImageUrl = uploaded.Url;
                coverImagePublicId = uploaded.PublicId;
            }

            var collection = new Collection
            {
                Id = Guid.NewGuid(),
                ProfileId = ownerId,
                Title = dto.Title,
                Description = dto.Description,
                CoverImageUrl = coverImageUrl,
                CoverImagePublicId = coverImagePublicId,
                PrivacyLevel = dto.PrivacyLevel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _uow.Collections.AddAsync(collection);
            await _uow.CompleteAsync();

            var reactions = await BuildReactionSummaryAsync(collection.Id, ownerId);

            return new CollectionResponse(
                collection.Id,
                collection.ProfileId,
                collection.Title,
                collection.Description,
                collection.CoverImageUrl ?? string.Empty,
                collection.PrivacyLevel,
                collection.CreatedAt,
                collection.UpdatedAt,
                new List<Guid>(),
                0,
                reactions
            );
        }

        public async Task<CollectionResponse?> UpdateAsync(Guid collectionId, Guid requesterId, UpdateCollectionRequest dto, IFormFile? coverImage)
        {
            var collection = await _uow.Collections.GetByIdAsync(collectionId);
            if (collection is null || collection.ProfileId != requesterId) return null;

            // Handle cover image update if provided
            if (coverImage != null && coverImage.Length > 0)
            {
                // Validate file type
                if (!coverImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Cover image must be an image file.");
                }

                // Delete old cover image from Cloudinary if it exists
                if (!string.IsNullOrWhiteSpace(collection.CoverImagePublicId))
                {
                    await _cloudinary.TryDeleteAsync(collection.CoverImagePublicId);
                }

                // Upload new cover image
                var uploaded = await _cloudinary.TryUploadAsync(coverImage, folder: "favi_collections");
                if (uploaded == null)
                {
                    throw new InvalidOperationException($"Failed to upload cover image: {coverImage.FileName}");
                }

                collection.CoverImageUrl = uploaded.Url;
                collection.CoverImagePublicId = uploaded.PublicId;
            }

            if (!string.IsNullOrWhiteSpace(dto.Title)) collection.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Description)) collection.Description = dto.Description;
            collection.PrivacyLevel = dto.PrivacyLevel ?? collection.PrivacyLevel;
            collection.UpdatedAt = DateTime.UtcNow;

            _uow.Collections.Update(collection);
            await _uow.CompleteAsync();

            var reactions = await BuildReactionSummaryAsync(collection.Id, requesterId);

            return new CollectionResponse(
                collection.Id,
                collection.ProfileId,
                collection.Title,
                collection.Description,
                collection.CoverImageUrl ?? string.Empty,
                collection.PrivacyLevel,
                collection.CreatedAt,
                collection.UpdatedAt,
                new List<Guid>(),
                0,
                reactions
            );
        }

        public async Task<bool> DeleteAsync(Guid collectionId, Guid requesterId)
        {
            var collection = await _uow.Collections.GetByIdAsync(collectionId);
            if (collection is null || collection.ProfileId != requesterId) return false;

            // Delete cover image from Cloudinary if it exists
            if (!string.IsNullOrWhiteSpace(collection.CoverImagePublicId))
            {
                await _cloudinary.TryDeleteAsync(collection.CoverImagePublicId);
            }

            _uow.Collections.Remove(collection);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<PagedResult<CollectionResponse>> GetByOwnerAsync(Guid ownerId, int page, int pageSize, Guid? currentUserId = null)
        {
            var skip = (page - 1) * pageSize;
            var (collections, total) = await _uow.Collections.GetAllByOwnerPagedAsync(ownerId, skip, pageSize);

            var dtos = new List<CollectionResponse>();
            foreach (var c in collections)
            {
                var postIds = c.PostCollections?.Select(pc => pc.PostId).ToList() ?? new List<Guid>();
                var reactions = await BuildReactionSummaryAsync(c.Id, currentUserId);

                dtos.Add(new CollectionResponse(
                    c.Id,
                    c.ProfileId,
                    c.Title,
                    c.Description,
                    c.CoverImageUrl ?? string.Empty,
                    c.PrivacyLevel,
                    c.CreatedAt,
                    c.UpdatedAt,
                    postIds,
                    postIds.Count,
                    reactions
                ));
            }

            return new PagedResult<CollectionResponse>(dtos, page, pageSize, total);
        }

        public async Task<CollectionResponse?> GetByIdAsync(Guid collectionId, Guid? currentUserId = null)
        {
            var collection = await _uow.Collections.GetCollectionWithPostsAsync(collectionId);
            if (collection is null) return null;

            var postIds = collection.PostCollections?.Select(pc => pc.PostId) ?? new List<Guid>();
            var reactions = await BuildReactionSummaryAsync(collection.Id, currentUserId);

            return new CollectionResponse(
                collection.Id,
                collection.ProfileId,
                collection.Title,
                collection.Description,
                collection.CoverImageUrl ?? string.Empty,
                collection.PrivacyLevel,
                collection.CreatedAt,
                collection.UpdatedAt,
                postIds,
                postIds.Count(),
                reactions
            );
        }

        public async Task<bool> AddPostAsync(Guid collectionId, Guid postId, Guid requesterId)
        {
            var collection = await _uow.Collections.GetByIdAsync(collectionId);
            if (collection is null || collection.ProfileId != requesterId) return false;

            if (!await _uow.PostCollections.ExistsInCollectionAsync(postId, collectionId))
            {
                await _uow.PostCollections.AddAsync(new Models.Entities.JoinTables.PostCollection
                {
                    CollectionId = collectionId,
                    PostId = postId
                });
                await _uow.CompleteAsync();
            }

            return true;
        }

        public async Task<bool> RemovePostAsync(Guid collectionId, Guid postId, Guid requesterId)
        {
            var collection = await _uow.Collections.GetByIdAsync(collectionId);
            if (collection is null || collection.ProfileId != requesterId) return false;

            await _uow.PostCollections.RemoveFromCollectionAsync(postId, collectionId);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<PagedResult<PostResponse>> GetPostsAsync(Guid collectionId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;

            var (posts, total) = await _uow.Posts.GetPostsByCollectionPagedAsync(collectionId, skip, pageSize);

            var dtos = posts.Select(p =>
            {
                var medias = p.PostMedias?.Select(m =>
                    new PostMediaResponse(
                        m.Id,
                        m.PostId ?? Guid.Empty,
                        m.Url,
                        m.PublicId,
                        m.Width,
                        m.Height,
                        m.Format,
                        m.Position,
                        m.ThumbnailUrl
                    )
                ).ToList() ?? new List<PostMediaResponse>();

                var tags = p.PostTags?.Select(pt =>
                    new TagDto(pt.Tag.Id, pt.Tag.Name)
                ).ToList() ?? new List<TagDto>();

                int totalReactions = p.Reactions?.Count ?? 0;
                var reactionCounts = p.Reactions?
                    .GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => g.Count())
                    ?? new Dictionary<ReactionType, int>();

                ReactionType? userReaction = null; // sau này truyền userId hiện tại vào nếu cần

                var reactionSummary = new ReactionSummaryDto(totalReactions, reactionCounts, userReaction);

                // 🆕 map location đúng Post + PostResponse mới
                LocationDto? location = null;
                if (p.LocationName != null ||
                    p.LocationFullAddress != null ||
                    p.LocationLatitude != null ||
                    p.LocationLongitude != null)
                {
                    location = new LocationDto(
                        p.LocationName,
                        p.LocationFullAddress,
                        p.LocationLatitude,
                        p.LocationLongitude
                    );
                }

                return new PostResponse(
                    p.Id,
                    p.ProfileId,
                    p.Caption,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.Privacy,
                    medias,
                    tags,
                    reactionSummary,
                    p.Comments.Count,
                    location
                );
            });

            return new PagedResult<PostResponse>(dtos, page, pageSize, total);
        }

        public async Task<Collection?> GetEntityByIdAsync(Guid id)
        {
            return await _uow.Collections.GetByIdAsync(id);
        }

        // ===== Reaction Methods =====

        public async Task<ReactionSummaryDto> GetReactionsAsync(Guid collectionId, Guid? currentUserId)
        {
            return await BuildReactionSummaryAsync(collectionId, currentUserId);
        }

        public async Task<ReactionType?> ToggleReactionAsync(Guid collectionId, Guid userId, ReactionType type)
        {
            var collection = await _uow.Collections.GetByIdAsync(collectionId);
            if (collection is null) return null;

            var existing = await _uow.Reactions.GetProfileReactionOnCollectionAsync(userId, collectionId);

            if (existing is null)
            {
                // Add new reaction
                await _uow.Reactions.AddAsync(new Models.Entities.JoinTables.Reaction
                {
                    CollectionId = collectionId,
                    ProfileId = userId,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                });
                await _uow.CompleteAsync();
                return type;
            }

            if (existing.Type == type)
            {
                // Remove reaction (same type clicked again)
                _uow.Reactions.Remove(existing);
                await _uow.CompleteAsync();
                return null;
            }

            // Change reaction type
            existing.Type = type;
            _uow.Reactions.Update(existing);
            await _uow.CompleteAsync();
            return type;
        }

        private async Task<ReactionSummaryDto> BuildReactionSummaryAsync(Guid collectionId, Guid? currentUserId)
        {
            var reactions = await _uow.Reactions.GetReactionsByCollectionIdAsync(collectionId);

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

        // ===== Trending Algorithm =====

        private const double W_Like = 1.0;
        private const double W_Comment = 3.0;
        private const double W_Share = 5.0;
        private const double W_View = 0.2;
        private const double Lambda = 0.1;
        private const double Beta = 0.5;
        private const double MaxAgeHours = 720;
        private static readonly TimeSpan VelocityWindow = TimeSpan.FromHours(1);
        private const int TrendingCandidateLimit = 500;

        public async Task<PagedResult<CollectionResponse>> GetTrendingCollectionsAsync(int page, int pageSize, Guid? currentUserId = null)
        {
            // Get candidate collections (up to limit)
            var skip = 0;
            var take = TrendingCandidateLimit;
            var (candidates, _) = await _uow.Collections.GetAllPagedAsync(skip, take);

            var now = DateTime.UtcNow;
            var scored = new List<(Collection Collection, double Score)>();

            foreach (var collection in candidates)
            {
                // Age filter (72 hours max)
                var ageHours = (now - collection.CreatedAt).TotalHours;
                if (ageHours > MaxAgeHours) continue;

                // Calculate trending score
                var score = await ComputeCollectionTrendingScoreAsync(collection);

                scored.Add((collection, score));
            }

            // Sort by score (descending)
            var ordered = scored
                .OrderByDescending(x => x.Score)
                .ToList();

            // Paginate
            var total = ordered.Count;
            var pageItems = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Collection)
                .ToList();

            var responses = new List<CollectionResponse>();
            foreach (var c in pageItems)
            {
                var postIds = c.PostCollections?.Select(pc => pc.PostId).ToList() ?? new List<Guid>();
                var reactions = await BuildReactionSummaryAsync(c.Id, currentUserId);

                responses.Add(new CollectionResponse(
                    c.Id,
                    c.ProfileId,
                    c.Title,
                    c.Description,
                    c.CoverImageUrl ?? string.Empty,
                    c.PrivacyLevel,
                    c.CreatedAt,
                    c.UpdatedAt,
                    postIds,
                    postIds.Count,
                    reactions
                ));
            }

            return new PagedResult<CollectionResponse>(responses, page, pageSize, total);
        }

        private async Task<double> ComputeCollectionTrendingScoreAsync(Collection collection)
        {
            var now = DateTime.UtcNow;

            // Age-based filtering
            var ageHours = Math.Max((now - collection.CreatedAt).TotalHours, 0);
            if (ageHours > MaxAgeHours)
                return 0;

            // Get engagement metrics
            var reactions = await _uow.Reactions.GetReactionsByCollectionIdAsync(collection.Id);
            var likeCount = reactions.Count();
            var commentCount = 0; // Collections don't have comments
            var shareCount = 0;   // Not yet tracked
            var viewCount = 0;    // Not yet tracked

            // Base engagement (minimum 1.0 for new collections with no interaction)
            var engagement =
                1.0 +
                W_Like * likeCount +
                W_Comment * commentCount +
                W_Share * shareCount +
                W_View * viewCount;

            // Time decay factor (exponential decay)
            var decay = Math.Exp(-Lambda * ageHours);

            // Velocity calculation (recency boost)
            var from = now - VelocityWindow;
            var recentReactions = reactions.Count(r => r.CreatedAt >= from);

            var deltaE = recentReactions;
            var velocity = VelocityWindow.TotalHours > 0
                ? deltaE / VelocityWindow.TotalHours
                : 0;

            var score = engagement * decay * (1 + Beta * velocity);
            return score;
        }
    }
}
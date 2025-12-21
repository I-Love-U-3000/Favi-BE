using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using System.Linq;

namespace Favi_BE.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly IUnitOfWork _uow;

        public CollectionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CollectionResponse> CreateAsync(Guid ownerId, CreateCollectionRequest dto)
        {
            var collection = new Collection
            {
                Id = Guid.NewGuid(),
                ProfileId = ownerId,
                Title = dto.Title,
                Description = dto.Description,
                CoverImageUrl = dto.CoverImageUrl,
                PrivacyLevel = dto.PrivacyLevel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _uow.Collections.AddAsync(collection);
            await _uow.CompleteAsync();

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
                0
            );
        }

        public async Task<CollectionResponse?> UpdateAsync(Guid collectionId, Guid requesterId, UpdateCollectionRequest dto)
        {
            var collection = await _uow.Collections.GetByIdAsync(collectionId);
            if (collection is null || collection.ProfileId != requesterId) return null;

            if (!string.IsNullOrWhiteSpace(dto.Title)) collection.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Description)) collection.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl)) collection.CoverImageUrl = dto.CoverImageUrl;
            collection.PrivacyLevel = dto.PrivacyLevel ?? collection.PrivacyLevel;
            collection.UpdatedAt = DateTime.UtcNow;

            _uow.Collections.Update(collection);
            await _uow.CompleteAsync();

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
                0
            );
        }

        public async Task<bool> DeleteAsync(Guid collectionId, Guid requesterId)
        {
            var collection = await _uow.Collections.GetByIdAsync(collectionId);
            if (collection is null || collection.ProfileId != requesterId) return false;

            _uow.Collections.Remove(collection);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<PagedResult<CollectionResponse>> GetByOwnerAsync(Guid ownerId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var (collections, total) = await _uow.Collections.GetAllByOwnerPagedAsync(ownerId, skip, pageSize);

            var dtos = collections.Select(c =>
            {
                var postIds = c.PostCollections?.Select(pc => pc.PostId).ToList() ?? new List<Guid>();
                return new CollectionResponse(
                    c.Id,
                    c.ProfileId,
                    c.Title,
                    c.Description,
                    c.CoverImageUrl ?? string.Empty,
                    c.PrivacyLevel,
                    c.CreatedAt,
                    c.UpdatedAt,
                    postIds,
                    postIds.Count
                );
            });

            return new PagedResult<CollectionResponse>(dtos, page, pageSize, total);
        }

        public async Task<CollectionResponse?> GetByIdAsync(Guid collectionId)
        {
            var collection = await _uow.Collections.GetCollectionWithPostsAsync(collectionId);
            if (collection is null) return null;

            var postIds = collection.PostCollections?.Select(pc => pc.PostId) ?? new List<Guid>();

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
                postIds.Count()
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
    }
}
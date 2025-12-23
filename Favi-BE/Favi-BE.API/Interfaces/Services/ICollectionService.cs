using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace Favi_BE.Interfaces.Services
{
    public interface ICollectionService
    {
        Task<CollectionResponse> CreateAsync(Guid ownerId, CreateCollectionRequest dto, IFormFile? coverImage);
        Task<CollectionResponse?> UpdateAsync(Guid collectionId, Guid requesterId, UpdateCollectionRequest dto, IFormFile? coverImage);
        Task<bool> DeleteAsync(Guid collectionId, Guid requesterId);

        Task<PagedResult<CollectionResponse>> GetByOwnerAsync(Guid ownerId, int page, int pageSize, Guid? currentUserId = null);
        Task<CollectionResponse?> GetByIdAsync(Guid collectionId, Guid? currentUserId = null);

        Task<bool> AddPostAsync(Guid collectionId, Guid postId, Guid requesterId);
        Task<bool> RemovePostAsync(Guid collectionId, Guid postId, Guid requesterId);

        Task<PagedResult<PostResponse>> GetPostsAsync(Guid collectionId, int page, int pageSize);
        Task<Collection?> GetEntityByIdAsync(Guid collectionId);

        Task<ReactionSummaryDto> GetReactionsAsync(Guid collectionId, Guid? currentUserId);
        Task<ReactionType?> ToggleReactionAsync(Guid collectionId, Guid userId, ReactionType type);
        Task<PagedResult<CollectionResponse>> GetTrendingCollectionsAsync(int page, int pageSize, Guid? currentUserId = null);
    }

}

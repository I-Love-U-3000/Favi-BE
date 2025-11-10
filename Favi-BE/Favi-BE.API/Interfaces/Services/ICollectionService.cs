using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;

namespace Favi_BE.Interfaces.Services
{
    public interface ICollectionService
    {
        Task<CollectionResponse> CreateAsync(Guid ownerId, CreateCollectionRequest dto);
        Task<CollectionResponse?> UpdateAsync(Guid collectionId, Guid requesterId, UpdateCollectionRequest dto);
        Task<bool> DeleteAsync(Guid collectionId, Guid requesterId);

        Task<PagedResult<CollectionResponse>> GetByOwnerAsync(Guid ownerId, int page, int pageSize);
        Task<CollectionResponse?> GetByIdAsync(Guid collectionId);

        Task<bool> AddPostAsync(Guid collectionId, Guid postId, Guid requesterId);
        Task<bool> RemovePostAsync(Guid collectionId, Guid postId, Guid requesterId);

        Task<PagedResult<PostResponse>> GetPostsAsync(Guid collectionId, int page, int pageSize);
        Task<Collection?> GetEntityByIdAsync(Guid collectionId);
    }

}

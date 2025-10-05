using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces
{
    public interface ITagService
    {
        Task<TagResponse> CreateAsync(string name);
        Task<IEnumerable<TagResponse>> GetAllAsync();
        Task<TagResponse?> GetByIdAsync(Guid id);
        Task<PagedResult<PostResponse>> GetPostsByTagAsync(Guid tagId, int page, int pageSize);
    }

}

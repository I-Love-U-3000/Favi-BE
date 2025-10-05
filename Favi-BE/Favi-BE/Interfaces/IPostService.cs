using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces
{
    public interface IPostService
    {
        Task<PostResponse?> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<PagedResult<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize);
        Task<PostResponse> CreateAsync(Guid authorId, string? caption, IEnumerable<string>? tags);
        Task<bool> UpdateAsync(Guid postId, Guid requesterId, string? caption);
        Task<bool> DeleteAsync(Guid postId, Guid requesterId);

        // Media
        Task<IEnumerable<PostMediaResponse>> UploadMediaAsync(Guid postId, IEnumerable<IFormFile> files, Guid requesterId);
        Task<bool> RemoveMediaAsync(Guid postId, Guid mediaId, Guid requesterId);

        // Tags
        Task<IEnumerable<TagDto>> AddTagsAsync(Guid postId, IEnumerable<string> tags, Guid requesterId);
        Task<bool> RemoveTagAsync(Guid postId, Guid tagId, Guid requesterId);

        // Reactions
        Task<ReactionSummaryDto> GetReactionsAsync(Guid postId, Guid? currentUserId);
        Task<ReactionType?> ToggleReactionAsync(Guid postId, Guid userId, ReactionType type);
    }

}

using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace Favi_BE.Interfaces.Services
{
    public interface IPostService
    {
        Task<PostResponse?> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<PostResponse> CreateAsync(Guid authorId, string? caption, IEnumerable<string>? tags, PrivacyLevel privacyLevel,
            LocationDto? location);

        Task<PostResponse> CreateAsync(Guid authorId, string? caption, IEnumerable<string>? tags, PrivacyLevel privacyLevel,
            LocationDto? location, List<IFormFile>? mediaFiles);
        Task<bool> UpdateAsync(Guid postId, Guid requesterId, string? caption);
        Task<bool> DeleteAsync(Guid postId, Guid requesterId);
        Task<bool> RestoreAsync(Guid postId, Guid requesterId);
        Task<bool> PermanentDeleteAsync(Guid postId, Guid requesterId);
        Task<bool> ArchiveAsync(Guid postId, Guid requesterId);
        Task<bool> UnarchiveAsync(Guid postId, Guid requesterId);
        Task<PagedResult<PostResponse>> GetRecycleBinAsync(Guid userId, int page, int pageSize);
        Task<PagedResult<PostResponse>> GetArchivedAsync(Guid userId, int page, int pageSize);

        // Media
        Task<IEnumerable<PostMediaResponse>> UploadMediaAsync(Guid postId, IEnumerable<IFormFile> files, Guid requesterId);
        Task<bool> RemoveMediaAsync(Guid postId, Guid mediaId, Guid requesterId);

        // Tags
        Task<IEnumerable<TagDto>> AddTagsAsync(Guid postId, IEnumerable<string> tags, Guid requesterId);
        Task<bool> RemoveTagAsync(Guid postId, Guid tagId, Guid requesterId);

        // Reactions
        Task<ReactionSummaryDto> GetReactionsAsync(Guid postId, Guid? currentUserId);
        Task<ReactionType?> ToggleReactionAsync(Guid postId, Guid userId, ReactionType type);
        Task<IEnumerable<PostReactorResponse>> GetReactorsAsync(Guid postId, Guid requesterId);
        Task<PagedResult<PostResponse>> GetByProfileAsync(Guid profileId, Guid? viewerId, int page, int pageSize);
        Task<PagedResult<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize);
        Task<PagedResult<FeedItemDto>> GetFeedWithRepostsAsync(Guid currentUserId, int page, int pageSize);
        Task<PagedResult<PostResponse>> GetGuestFeedAsync(int page, int pageSize);
        Task<PagedResult<PostResponse>> GetExploreAsync(Guid userId, int page, int pageSize);
        Task<PagedResult<PostResponse>> GetLatestAsync(int page, int pageSize);
        Task<Post?> GetEntityAsync(Guid id);
        Task<bool> AdminDeleteAsync(Guid postId, Guid adminId, string reason);

        // Repost/Share
        Task<RepostResponse?> SharePostAsync(Guid postId, Guid sharerId, string? caption);
        Task<bool> UnsharePostAsync(Guid postId, Guid sharerId);
        Task<RepostResponse?> GetRepostAsync(Guid repostId, Guid? currentUserId);
        Task<PagedResult<RepostResponse>> GetRepostsByProfileAsync(Guid profileId, Guid? currentUserId, int page, int pageSize);
    }

}

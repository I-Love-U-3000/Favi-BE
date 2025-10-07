using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;

namespace Favi_BE.Interfaces.Services
{
    public interface IProfileService
    {
        Task<ProfileResponse?> GetByIdAsync(Guid profileId);
        Task<ProfileResponse?> UpdateAsync(Guid profileId, ProfileUpdateRequest dto);
        Task<bool> FollowAsync(Guid followerId, Guid followeeId);
        Task<bool> UnfollowAsync(Guid followerId, Guid followeeId);
        Task<IEnumerable<FollowResponse>> GetFollowersAsync(Guid profileId);
        Task<IEnumerable<FollowResponse>> GetFollowingsAsync(Guid profileId);
        Task<IEnumerable<FollowResponse>> GetFollowersAsync(Guid profileId, int skip, int take);
        Task<IEnumerable<FollowResponse>> GetFollowingsAsync(Guid profileId, int skip, int take);
        Task<IEnumerable<SocialLinkDto>> GetSocialLinksAsync(Guid profileId);
        Task<SocialLinkDto> AddSocialLinkAsync(Guid profileId, SocialLinkDto dto);
        Task<bool> RemoveSocialLinkAsync(Guid profileId, Guid linkId);
        Task<Profile> CreateProfileAsync(Guid id, string username, string? displayName);
        Task<bool> DeleteAsync(Guid profileId);
    }

}

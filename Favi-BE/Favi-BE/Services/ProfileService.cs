using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;

namespace Favi_BE.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _uow;

        public ProfileService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ProfileResponse?> GetByIdAsync(Guid profileId)
        {
            var profile = await _uow.Profiles.GetByIdAsync(profileId);
            if (profile is null) return null;

            var followers = await _uow.Follows.GetFollowersCountAsync(profileId);
            var followings = await _uow.Follows.GetFollowingCountAsync(profileId);

            return new ProfileResponse(
                profile.Id,
                profile.Username,
                profile.DisplayName,
                profile.Bio,
                profile.AvatarUrl,
                profile.CoverUrl,
                profile.CreatedAt,
                profile.LastActiveAt ?? DateTime.MinValue,
                profile.PrivacyLevel,
                profile.FollowPrivacyLevel,
                followers,
                followings
            );
        }

        public async Task<ProfileResponse?> UpdateAsync(Guid profileId, ProfileUpdateRequest dto)
        {
            var profile = await _uow.Profiles.GetByIdAsync(profileId);
            if (profile is null) return null;

            if (!string.IsNullOrWhiteSpace(dto.DisplayName))
                profile.DisplayName = dto.DisplayName;
            if (!string.IsNullOrWhiteSpace(dto.Bio))
                profile.Bio = dto.Bio;
            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
                profile.AvatarUrl = dto.AvatarUrl;
            if (!string.IsNullOrWhiteSpace(dto.CoverUrl))
                profile.CoverUrl = dto.CoverUrl;
            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != profile.Username)
                profile.Username = dto.Username;
            if (dto.PrivacyLevel.HasValue)
                profile.PrivacyLevel = dto.PrivacyLevel.Value;
            if (dto.FollowPrivacyLevel.HasValue)
                profile.FollowPrivacyLevel = dto.FollowPrivacyLevel.Value;
            profile.LastActiveAt = DateTime.UtcNow;
            _uow.Profiles.Update(profile);

            await _uow.CompleteAsync();
            return await GetByIdAsync(profileId);
        }

        public async Task<bool> FollowAsync(Guid followerId, Guid followeeId)
        {
            if (followerId == followeeId) return false;
            if (await _uow.Follows.IsFollowingAsync(followerId, followeeId)) return true;

            await _uow.Follows.AddAsync(new Models.Entities.JoinTables.Follow
            {
                FollowerId = followerId,
                FolloweeId = followeeId,
                CreatedAt = DateTime.UtcNow
            });
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<bool> UnfollowAsync(Guid followerId, Guid followeeId)
        {
            var follow = await _uow.Follows.GetAsync(followerId, followeeId);
            if (follow is null) return false;

            _uow.Follows.Remove(follow);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<IEnumerable<FollowResponse>> GetFollowersAsync(Guid profileId)
        {
            var result = await _uow.Follows.GetFollowersAsync(profileId, 0, 1000);
            return result.Select(f => new FollowResponse(f.FollowerId, f.FolloweeId, f.CreatedAt));
        }

        public async Task<IEnumerable<FollowResponse>> GetFollowersAsync(Guid profileId, int skip, int take)
        {
            var result = await _uow.Follows.GetFollowersAsync(profileId, skip, take);
            return result.Select(f => new FollowResponse(f.FollowerId, f.FolloweeId, f.CreatedAt));
        }

        public async Task<IEnumerable<FollowResponse>> GetFollowingsAsync(Guid profileId, int skip, int take)
        {
            var result = await _uow.Follows.GetFollowingAsync(profileId, skip, take);
            return result.Select(f => new FollowResponse(f.FollowerId, f.FolloweeId, f.CreatedAt));
        }
        public async Task<IEnumerable<FollowResponse>> GetFollowingsAsync(Guid profileId)
        {
            var result = await _uow.Follows.GetFollowingAsync(profileId, 0, 1000);
            return result.Select(f => new FollowResponse(f.FollowerId, f.FolloweeId, f.CreatedAt));
        }
        public async Task<IEnumerable<SocialLinkDto>> GetSocialLinksAsync(Guid profileId)
        {
            var links = await _uow.SocialLinks.GetByProfileIdAsync(profileId);
            return links.Select(l => new SocialLinkDto(l.Id, l.Kind, l.Url));
        }

        public async Task<SocialLinkDto> AddSocialLinkAsync(Guid profileId, SocialLinkDto dto)
        {
            var entity = new SocialLink
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                Kind = dto.SocialKind,
                Url = dto.Url,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.SocialLinks.AddAsync(entity);
            await _uow.CompleteAsync();

            return new SocialLinkDto(entity.Id, entity.Kind, entity.Url);
        }

        public async Task<bool> RemoveSocialLinkAsync(Guid profileId, Guid linkId)
        {
            var link = await _uow.SocialLinks.GetByIdAsync(linkId);
            if (link is null || link.ProfileId != profileId) return false;

            _uow.SocialLinks.Remove(link);
            await _uow.CompleteAsync();
            return true;
        }
        public async Task<Profile> CreateProfileAsync(Guid id, string username, string? displayName)
        {
            var profile = new Profile
            {
                Id = id,
                Username = username,
                DisplayName = displayName ?? username,
                CreatedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };
            await _uow.Profiles.AddAsync(profile);
            await _uow.CompleteAsync();
            return profile;
        }

        public async Task<bool> DeleteAsync(Guid profileId)
        {
            var profile = await _uow.Profiles.GetByIdAsync(profileId);
            if (profile is null) return false;
            _uow.Profiles.Remove(profile);
            await _uow.CompleteAsync();
            return true;
        }
    }
}

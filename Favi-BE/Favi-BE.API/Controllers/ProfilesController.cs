using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Auth.Application.Commands.DeleteProfile;
using Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;
using Favi_BE.Modules.Auth.Application.Commands.UpdateProfile;
using Favi_BE.Modules.Auth.Application.Commands.UploadAvatar;
using Favi_BE.Modules.Auth.Application.Commands.UploadPoster;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using Favi_BE.Modules.Auth.Application.Contracts.WriteModels;
using Favi_BE.Modules.Auth.Application.Queries.GetOnlineFriends;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileAvatar;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileById;
using Favi_BE.Modules.Auth.Application.Queries.GetProfilePoster;
using Favi_BE.Modules.Auth.Application.Queries.GetRecommendedProfiles;
using Favi_BE.Modules.SocialGraph.Application.Commands.AddSocialLink;
using Favi_BE.Modules.SocialGraph.Application.Commands.FollowUser;
using Favi_BE.Modules.SocialGraph.Application.Commands.RemoveSocialLink;
using Favi_BE.Modules.SocialGraph.Application.Commands.UnfollowUser;
using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowers;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowings;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetSocialLinks;
using Favi_BE.Modules.SocialGraph.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilesController : ControllerBase
    {
        private readonly IAuthFacade _authFacade;
        private readonly ISocialGraphFacade _socialFacade;
        private readonly ICloudinaryService _cloudinary;

        public ProfilesController(
            IAuthFacade authFacade,
            ISocialGraphFacade socialFacade,
            ICloudinaryService cloudinary)
        {
            _authFacade = authFacade;
            _socialFacade = socialFacade;
            _cloudinary = cloudinary;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProfileResponse>> GetById(Guid id)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var profile = await _authFacade.GetProfileByIdAsync(new GetProfileByIdQuery(id, viewerId));
            if (profile is null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại hoặc bạn không có quyền xem." });

            return Ok(MapProfile(profile));
        }

        [Authorize]
        [HttpPut]
        public async Task<ActionResult<ProfileResponse>> Update(ProfileUpdateRequest dto)
        {
            var userId = User.GetUserId();
            var result = await _authFacade.UpdateProfileAsync(new UpdateProfileCommand(
                userId,
                dto.Username,
                dto.DisplayName,
                dto.Bio,
                dto.AvatarUrl,
                dto.CoverUrl,
                dto.PrivacyLevel.HasValue ? (int)dto.PrivacyLevel.Value : null,
                dto.FollowPrivacyLevel.HasValue ? (int)dto.FollowPrivacyLevel.Value : null));

            return result.Succeeded
                ? Ok(MapProfile(result.Profile!))
                : NotFound(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpPost("follow/{targetId}")]
        public async Task<IActionResult> Follow(Guid targetId)
        {
            var userId = User.GetUserId();
            var result = await _socialFacade.FollowUserAsync(new FollowUserCommand(userId, targetId));
            return result.Succeeded
                ? Ok(new { message = "Đã theo dõi." })
                : BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpDelete("follow/{targetId}")]
        public async Task<IActionResult> Unfollow(Guid targetId)
        {
            var userId = User.GetUserId();
            var result = await _socialFacade.UnfollowUserAsync(new UnfollowUserCommand(userId, targetId));
            return result.Succeeded
                ? Ok(new { message = "Đã bỏ theo dõi." })
                : BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [HttpGet("{id}/followers")]
        [HttpGet("{id}/followers/search")]
        public async Task<ActionResult<PaginationResult<FollowResponse>>> Followers(
            Guid id,
            [FromQuery] string? query = null,
            [FromQuery] string? username = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null,
            [FromQuery] int? pageSize = null)
        {
            var actualPage = page > 0 ? page : 1;
            var actualSize = pageSize ?? (take ?? (size > 0 ? size : 10));
            var actualSkip = skip ?? ((actualPage - 1) * actualSize);
            var searchQuery = query ?? username ?? search;

            var (items, total) = await _socialFacade.GetFollowersAsync(new GetFollowersQuery(id, actualSkip, actualSize, searchQuery));
            var dtos = items.Select(f => new FollowResponse(
                f.FollowerId,
                f.FolloweeId,
                f.CreatedAt,
                f.Username,
                f.DisplayName,
                f.AvatarUrl,
                f.Bio)).ToList();
            return Ok(PaginationResult<FollowResponse>.Create(dtos, actualPage, actualSize, total));
        }

        [HttpGet("{id}/followings")]
        [HttpGet("{id}/followings/search")]
        public async Task<ActionResult<PaginationResult<FollowResponse>>> Followings(
            Guid id,
            [FromQuery] string? query = null,
            [FromQuery] string? username = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null,
            [FromQuery] int? pageSize = null)
        {
            var actualPage = page > 0 ? page : 1;
            var actualSize = pageSize ?? (take ?? (size > 0 ? size : 10));
            var actualSkip = skip ?? ((actualPage - 1) * actualSize);
            var searchQuery = query ?? username ?? search;

            var (items, total) = await _socialFacade.GetFollowingsAsync(new GetFollowingsQuery(id, actualSkip, actualSize, searchQuery));
            var dtos = items.Select(f => new FollowResponse(
                f.FollowerId,
                f.FolloweeId,
                f.CreatedAt,
                f.Username,
                f.DisplayName,
                f.AvatarUrl,
                f.Bio)).ToList();
            return Ok(PaginationResult<FollowResponse>.Create(dtos, actualPage, actualSize, total));
        }

        [HttpGet("{id}/links")]
        public async Task<IActionResult> GetLinks(Guid id)
        {
            var result = await _socialFacade.GetSocialLinksAsync(new GetSocialLinksQuery(id));
            return Ok(result);
        }

        [Authorize]
        [HttpGet("me/links")]
        public async Task<IActionResult> GetLinks()
        {
            var userId = User.GetUserId();
            var result = await _socialFacade.GetSocialLinksAsync(new GetSocialLinksQuery(userId));
            return Ok(result);
        }

        [Authorize]
        [HttpPost("links")]
        public async Task<IActionResult> AddLink(SocialLinkDto dto)
        {
            var userId = User.GetUserId();
            var result = await _socialFacade.AddSocialLinkAsync(new AddSocialLinkCommand(userId, (Favi_BE.Modules.SocialGraph.Domain.SocialKind)(int)dto.SocialKind, dto.Url));
            return result.Succeeded
                ? Ok(result.Data)
                : BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpDelete("links/{linkId}")]
        public async Task<IActionResult> RemoveLink(Guid linkId)
        {
            var userId = User.GetUserId();
            var result = await _socialFacade.RemoveSocialLinkAsync(new RemoveSocialLinkCommand(userId, linkId));
            return result.Succeeded
                ? Ok(new { message = "Đã xoá liên kết mạng xã hội." })
                : NotFound(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var userId = User.GetUserId();
            var deleted = await _authFacade.DeleteProfileAsync(new DeleteProfileCommand(userId));
            return deleted
                ? Ok(new { message = "Đã xoá tài khoản." })
                : BadRequest(new { code = "DELETE_PROFILE_FAILED", message = "Không thể xoá tài khoản." });
        }

        [HttpGet("avatar/{profileId}")]
        public async Task<IActionResult> GetAvatar(Guid profileId)
        {
            var url = await _authFacade.GetProfileAvatarAsync(new GetProfileAvatarQuery(profileId));
            if (url is null)
                return NotFound(new { code = "AVATAR_NOT_FOUND", message = "Không tìm thấy ảnh đại diện." });
            return Ok(url);
        }

        [HttpGet("poster/{profileId}")]
        public async Task<IActionResult> GetPoster(Guid profileId)
        {
            var url = await _authFacade.GetProfilePosterAsync(new GetProfilePosterQuery(profileId));
            if (url is null)
                return NotFound(new { code = "POSTER_NOT_FOUND", message = "Không tìm thấy ảnh bìa." });
            return Ok(url);
        }

        [Authorize]
        [HttpPost("avatar")]
        public async Task<ActionResult<PostMediaResponse>> UploadAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { code = "NO_FILE", message = "Không có file nào được gửi." });

            var userId = User.GetUserId();

            var uploaded = await _cloudinary.TryUploadAsync(file);
            if (uploaded is null)
                return BadRequest(new { code = "UPLOAD_FAILED", message = "Upload avatar thất bại hoặc file không hợp lệ." });

            var result = await _authFacade.UploadAvatarAsync(new UploadAvatarCommand(
                userId,
                new UploadedImageData(uploaded.Url, uploaded.ThumbnailUrl, uploaded.PublicId, uploaded.Width, uploaded.Height, uploaded.Format)));

            if (!result.Succeeded)
                return result.ErrorCode == "PROFILE_NOT_FOUND"
                    ? NotFound(new { code = result.ErrorCode, message = "Hồ sơ không tồn tại." })
                    : BadRequest(new { code = result.ErrorCode, message = "Upload avatar thất bại." });

            return Ok(new PostMediaResponse(
                result.MediaId,
                Guid.Empty,
                result.Url!,
                result.PublicId!,
                result.Width,
                result.Height,
                result.Format!,
                0,
                result.ThumbnailUrl));
        }

        [Authorize]
        [HttpPost("poster")]
        public async Task<ActionResult<PostMediaResponse>> UploadPoster([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { code = "NO_FILE", message = "Không có file nào được gửi." });

            var userId = User.GetUserId();

            var uploaded = await _cloudinary.TryUploadAsync(file);
            if (uploaded is null)
                return BadRequest(new { code = "UPLOAD_FAILED", message = "Upload poster thất bại hoặc file không hợp lệ." });

            var result = await _authFacade.UploadPosterAsync(new UploadPosterCommand(
                userId,
                new UploadedImageData(uploaded.Url, uploaded.ThumbnailUrl, uploaded.PublicId, uploaded.Width, uploaded.Height, uploaded.Format)));

            if (!result.Succeeded)
                return result.ErrorCode == "PROFILE_NOT_FOUND"
                    ? NotFound(new { code = result.ErrorCode, message = "Hồ sơ không tồn tại." })
                    : BadRequest(new { code = result.ErrorCode, message = "Upload poster thất bại." });

            return Ok(new PostMediaResponse(
                result.MediaId,
                Guid.Empty,
                result.Url!,
                result.PublicId!,
                result.Width,
                result.Height,
                result.Format!,
                0,
                result.ThumbnailUrl));
        }

        [HttpGet("recommendations")]
        [Authorize]
        public async Task<ActionResult<PaginationResult<ProfileResponse>>> GetRecommendations(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? pageSize = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null)
        {
            var viewerId = User.GetUserId();
            var actualPage = page > 0 ? page : 1;
            var actualSize = pageSize.HasValue && pageSize.Value > 0 ? pageSize.Value : (size > 0 ? size : 10);
            var actualSkip = skip ?? ((actualPage - 1) * actualSize);
            var actualTake = take ?? actualSize;

            var items = await _authFacade.GetRecommendedProfilesAsync(new GetRecommendedProfilesQuery(viewerId, actualSkip, actualTake));
            var dtos = items.Select(MapProfile).ToList();
            var hasPrevious = actualPage > 1;
            var hasNext = dtos.Count >= actualTake;
            return Ok(new PaginationResult<ProfileResponse>(dtos, actualPage, actualSize, hasPrevious, hasNext));
        }

        [HttpGet("online-friends")]
        [Authorize]
        public async Task<ActionResult<PaginationResult<ProfileResponse>>> GetOnlineFriends(
            [FromQuery] int withinLastMinutes = 15,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? pageSize = null)
        {
            var actualPage = page > 0 ? page : 1;
            var actualSize = pageSize ?? (size > 0 ? size : 10);
            var userId = User.GetUserId();
            var items = await _authFacade.GetOnlineFriendsAsync(new GetOnlineFriendsQuery(userId, withinLastMinutes));
            var dtos = items.Select(MapProfile).ToList();
            var paginated = dtos.Skip((actualPage - 1) * actualSize).Take(actualSize).ToList();
            return Ok(PaginationResult<ProfileResponse>.Create(paginated, actualPage, actualSize, dtos.Count));
        }

        [HttpGet("friends")]
        [Authorize]
        public async Task<ActionResult<PaginationResult<ProfileResponse>>> GetMyFriends(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? pageSize = null)
        {
            var userId = User.GetUserId();
            return await GetFriendsInternal(userId, page, size, pageSize);
        }

        [HttpGet("{id}/friends")]
        public async Task<ActionResult<PaginationResult<ProfileResponse>>> GetFriends(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? pageSize = null)
        {
            return await GetFriendsInternal(id, page, size, pageSize);
        }

        private async Task<ActionResult<PaginationResult<ProfileResponse>>> GetFriendsInternal(
            Guid profileId, int page, int size, int? pageSize)
        {
            var actualPage = page > 0 ? page : 1;
            var actualSize = pageSize ?? (size > 0 ? size : 10);
            var actualSkip = (actualPage - 1) * actualSize;

            var (items, total) = await _socialFacade.GetFollowingsAsync(new GetFollowingsQuery(profileId, actualSkip, actualSize));
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var profiles = new List<ProfileResponse>();
            foreach (var f in items)
            {
                var p = await _authFacade.GetProfileByIdAsync(new GetProfileByIdQuery(f.FolloweeId, viewerId));
                if (p is not null)
                {
                    profiles.Add(MapProfile(p));
                }
            }
            return Ok(PaginationResult<ProfileResponse>.Create(profiles, actualPage, actualSize, total));
        }

        [HttpPost("heartbeat")]
        [Authorize]
        public async Task<IActionResult> Heartbeat()
        {
            var userId = User.GetUserId();
            var lastActiveAt = await _authFacade.UpdateLastActiveAsync(new UpdateLastActiveCommand(userId));
            return Ok(new { message = "Heartbeat recorded.", lastActiveAt });
        }

        private static ProfileResponse MapProfile(ProfileReadModel m) => new(
            m.Id,
            m.Username,
            m.DisplayName,
            m.Bio,
            m.AvatarUrl,
            m.CoverUrl,
            m.Email,
            m.CreatedAt,
            m.LastActiveAt,
            (PrivacyLevel)m.PrivacyLevel,
            (PrivacyLevel)m.FollowPrivacyLevel,
            m.IsBanned,
            m.BannedUntil,
            m.FollowersCount,
            m.FollowingCount);
    }
}

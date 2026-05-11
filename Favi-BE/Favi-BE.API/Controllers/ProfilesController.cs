using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Auth.Application.Commands.DeleteProfile;
using Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;
using Favi_BE.Modules.Auth.Application.Commands.UpdateProfile;
using Favi_BE.Modules.Auth.Application.Commands.UploadAvatar;
using Favi_BE.Modules.Auth.Application.Commands.UploadPoster;
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
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowers;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowings;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetSocialLinks;
using Favi_BE.Modules.SocialGraph.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICloudinaryService _cloudinary;

        public ProfilesController(IMediator mediator, ICloudinaryService cloudinary)
        {
            _mediator = mediator;
            _cloudinary = cloudinary;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProfileResponse>> GetById(Guid id)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var profile = await _mediator.Send(new GetProfileByIdQuery(id, viewerId));
            if (profile is null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại hoặc bạn không có quyền xem." });

            return Ok(MapProfile(profile));
        }

        [Authorize]
        [HttpPut]
        public async Task<ActionResult<ProfileResponse>> Update(ProfileUpdateRequest dto)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new UpdateProfileCommand(
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
            var result = await _mediator.Send(new FollowUserCommand(userId, targetId));
            return result.Succeeded
                ? Ok(new { message = "Đã theo dõi." })
                : BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpDelete("follow/{targetId}")]
        public async Task<IActionResult> Unfollow(Guid targetId)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new UnfollowUserCommand(userId, targetId));
            return result.Succeeded
                ? Ok(new { message = "Đã bỏ theo dõi." })
                : BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [HttpGet("{id}/followers")]
        public async Task<IActionResult> Followers(Guid id, [FromQuery] int? skip, [FromQuery] int? take)
        {
            var result = await _mediator.Send(new GetFollowersQuery(id, skip ?? 0, take ?? 1000));
            return Ok(result);
        }

        [HttpGet("{id}/followings")]
        public async Task<IActionResult> Followings(Guid id, [FromQuery] int? skip, [FromQuery] int? take)
        {
            var result = await _mediator.Send(new GetFollowingsQuery(id, skip ?? 0, take ?? 1000));
            return Ok(result);
        }

        [HttpGet("{id}/links")]
        public async Task<IActionResult> GetLinks(Guid id)
        {
            var result = await _mediator.Send(new GetSocialLinksQuery(id));
            return Ok(result);
        }

        [Authorize]
        [HttpGet("me/links")]
        public async Task<IActionResult> GetLinks()
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new GetSocialLinksQuery(userId));
            return Ok(result);
        }

        [Authorize]
        [HttpPost("links")]
        public async Task<IActionResult> AddLink(SocialLinkDto dto)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new AddSocialLinkCommand(userId, (Favi_BE.Modules.SocialGraph.Domain.SocialKind)(int)dto.SocialKind, dto.Url));
            return result.Succeeded
                ? Ok(result.Data)
                : BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpDelete("links/{linkId}")]
        public async Task<IActionResult> RemoveLink(Guid linkId)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new RemoveSocialLinkCommand(userId, linkId));
            return result.Succeeded
                ? Ok(new { message = "Đã xoá liên kết mạng xã hội." })
                : NotFound(new { code = result.ErrorCode, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var userId = User.GetUserId();
            var deleted = await _mediator.Send(new DeleteProfileCommand(userId));
            return deleted
                ? Ok(new { message = "Đã xoá tài khoản." })
                : BadRequest(new { code = "DELETE_PROFILE_FAILED", message = "Không thể xoá tài khoản." });
        }

        [HttpGet("avatar/{profileId}")]
        public async Task<IActionResult> GetAvatar(Guid profileId)
        {
            var url = await _mediator.Send(new GetProfileAvatarQuery(profileId));
            if (url is null)
                return NotFound(new { code = "AVATAR_NOT_FOUND", message = "Không tìm thấy ảnh đại diện." });
            return Ok(url);
        }

        [HttpGet("poster/{profileId}")]
        public async Task<IActionResult> GetPoster(Guid profileId)
        {
            var url = await _mediator.Send(new GetProfilePosterQuery(profileId));
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

            var result = await _mediator.Send(new UploadAvatarCommand(
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

            var result = await _mediator.Send(new UploadPosterCommand(
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
        public async Task<ActionResult<IEnumerable<ProfileResponse>>> GetRecommendations(
            [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var viewerId = User.GetUserId();
            var items = await _mediator.Send(new GetRecommendedProfilesQuery(viewerId, skip, take));
            return Ok(items.Select(MapProfile));
        }

        [HttpGet("online-friends")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProfileResponse>>> GetOnlineFriends(
            [FromQuery] int withinLastMinutes = 15)
        {
            var userId = User.GetUserId();
            var items = await _mediator.Send(new GetOnlineFriendsQuery(userId, withinLastMinutes));
            return Ok(items.Select(MapProfile));
        }

        [HttpPost("heartbeat")]
        [Authorize]
        public async Task<IActionResult> Heartbeat()
        {
            var userId = User.GetUserId();
            var lastActiveAt = await _mediator.Send(new UpdateLastActiveCommand(userId));
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

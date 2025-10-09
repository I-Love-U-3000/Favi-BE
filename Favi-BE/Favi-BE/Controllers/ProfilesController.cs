using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileService _profiles;
        private readonly IPrivacyGuard _privacy;

        public ProfilesController(IProfileService profiles) => _profiles = profiles;

        // Ai cũng xem được profile người khác
        [HttpGet("{id}")]
        public async Task<ActionResult<ProfileResponse>> GetById(Guid id)
        {
            var profile = await _profiles.GetEntityByIdAsync(id);
            if (profile is null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });

            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            if (!await _privacy.CanViewProfileAsync(profile, viewerId))
                return StatusCode(403, new { code = "PROFILE_FORBIDDEN", message = "Bạn không có quyền xem hồ sơ này." });

            return Ok(await _profiles.GetByIdAsync(id));
        }


        // Cập nhật profile chính mình
        [Authorize]
        [HttpPut]
        public async Task<ActionResult<ProfileResponse>> Update(ProfileUpdateRequest dto)
        {
            var userId = User.GetUserIdFromMetadata();
            var updated = await _profiles.UpdateAsync(userId, dto);
            return updated is null
                ? NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ để cập nhật." })
                : Ok(updated);
        }

        // Follow người khác
        [Authorize]
        [HttpPost("follow/{targetId}")]
        public async Task<IActionResult> Follow(Guid targetId)
        {
            var userId = User.GetUserIdFromMetadata();
            var profile = await _profiles.GetEntityByIdAsync(targetId);
            if (profile is null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ mục tiêu không tồn tại." });

            if (!await _privacy.CanFollowAsync(profile, userId))
                return StatusCode(403, new { code = "FOLLOW_FORBIDDEN", message = "Bạn không thể theo dõi hồ sơ này." });

            var ok = await _profiles.FollowAsync(userId, targetId);
            return ok ? Ok(new { message = "Đã theo dõi." }) : BadRequest(new { code = "FOLLOW_FAILED", message = "Theo dõi thất bại." });
        }

        // Unfollow người khác
        [Authorize]
        [HttpDelete("follow/{targetId}")]
        public async Task<IActionResult> Unfollow(Guid targetId)
        {
            var userId = User.GetUserIdFromMetadata();
            var ok = await _profiles.UnfollowAsync(userId, targetId);
            return ok ? Ok(new { message = "Đã bỏ theo dõi." }) : BadRequest(new { code = "UNFOLLOW_FAILED", message = "Bỏ theo dõi thất bại." });
        }

        // Xem followers của người khác
        [HttpGet("{id}/followers")]
        public async Task<IActionResult> Followers(Guid id, [FromQuery] int? skip, [FromQuery] int? take)
        {
            var profile = await _profiles.GetEntityByIdAsync(id);
            if (profile is null)
                return NotFound();
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            if (!await _privacy.CanViewFollowListAsync(profile, viewerId))
                return Forbid();

            int s = skip ?? 0;
            int t = take ?? 1000;
            var result = await _profiles.GetFollowersAsync(id, s, t);
            return Ok(result);
        }

        // Xem followings của người khác
        [HttpGet("{id}/followings")]
        public async Task<IActionResult> Followings(Guid id, [FromQuery] int? skip, [FromQuery] int? take)
        {
            var profile = await _profiles.GetEntityByIdAsync(id);
            if (profile is null)
                return NotFound();
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            if (!await _privacy.CanViewFollowListAsync(profile, viewerId))
                return Forbid();

            int s = skip ?? 0;
            int t = take ?? 1000;
            var result = await _profiles.GetFollowingsAsync(id, s, t);
            return Ok(result);
        }
        
        [HttpGet("{id}/links")]
        public async Task<IActionResult> GetLinks(Guid id)
        {
            var links = await _profiles.GetSocialLinksAsync(id);
            return Ok(links);
        }

        // Lấy social links của chính mình
        [Authorize]
        [HttpGet("me/links")]
        public async Task<IActionResult> GetLinks()
        {
            var userId = User.GetUserIdFromMetadata();
            return Ok(await _profiles.GetSocialLinksAsync(userId));
        }

        // Thêm social link cho chính mình
        [Authorize]
        [HttpPost("links")]
        public async Task<IActionResult> AddLink(SocialLinkDto dto)
        {
            var userId = User.GetUserIdFromMetadata();
            return Ok(await _profiles.AddSocialLinkAsync(userId, dto));
        }

        // Xoá social link của chính mình
        [Authorize]
        [HttpDelete("links/{linkId}")]
        public async Task<IActionResult> RemoveLink(Guid linkId)
        {
            var userId = User.GetUserIdFromMetadata();
            var ok = await _profiles.RemoveSocialLinkAsync(userId, linkId);
            return ok ? Ok() : NotFound();
        }

        // Xoá tài khoản chính mình
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var userId = User.GetUserIdFromMetadata();
            if (await _profiles.DeleteAsync(userId))
                return Ok();
            return BadRequest();
        }
    }
}

using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileService _profiles;
        private readonly IPrivacyGuard _privacy;

        public ProfilesController(IProfileService profiles, IPrivacyGuard privacy)
        { _profiles = profiles; _privacy = privacy; }

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
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });

            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            if (!await _privacy.CanViewFollowListAsync(profile, viewerId))
                return StatusCode(403, new { code = "FOLLOW_LIST_FORBIDDEN", message = "Bạn không có quyền xem danh sách người theo dõi." });

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
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });

            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            if (!await _privacy.CanViewFollowListAsync(profile, viewerId))
                return StatusCode(403, new { code = "FOLLOW_LIST_FORBIDDEN", message = "Bạn không có quyền xem danh sách đang theo dõi." });

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
            return ok
                ? Ok(new { message = "Đã xoá liên kết mạng xã hội." })
                : NotFound(new { code = "SOCIAL_LINK_NOT_FOUND", message = "Không tìm thấy liên kết để xoá." });
        }

        // Xoá tài khoản chính mình
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var userId = User.GetUserIdFromMetadata();
            return await _profiles.DeleteAsync(userId)
                ? Ok(new { message = "Đã xoá tài khoản." })
                : BadRequest(new { code = "DELETE_PROFILE_FAILED", message = "Không thể xoá tài khoản." });
        }

        //I dont think we need this endpoint, but I will keep it for now
        [HttpGet("avatar/{profileId}")]
        public async Task<IActionResult> GetAvatar(Guid profileId)
        {
            var avatar = await _profiles.GetAvatar(profileId);
            if (avatar is null)
                return NotFound(new { code = "AVATAR_NOT_FOUND", message = "Không tìm thấy ảnh đại diện." });
            return Ok(avatar.Url);
        }

        //I dont think we need this endpoint, but I will keep it for now
        [HttpGet("poster/{profileId}")]
        public async Task<IActionResult> GetPoster(Guid profileId)
        {
            var poster = await _profiles.GetPoster(profileId);
            if (poster is null)
                return NotFound(new { code = "POSTER_NOT_FOUND", message = "Không tìm thấy ảnh bìa." });
            return Ok(poster.Url);
        }


        [Authorize]
        [HttpPost("avatar")]
        public async Task<ActionResult<PostMediaResponse>> UploadAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { code = "NO_FILE", message = "Không có file nào được gửi." });

            var userId = User.GetUserIdFromMetadata();

            // Đảm bảo profile tồn tại
            var profile = await _profiles.GetEntityByIdAsync(userId);
            if (profile is null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });

            var media = await _profiles.UploadAvatarAsync(userId, file);

            if (media is null)
                return BadRequest(new { code = "UPLOAD_FAILED", message = "Upload avatar thất bại hoặc file không hợp lệ." });

            return Ok(media);
        }

        [Authorize]
        [HttpPost("poster")]
        public async Task<ActionResult<PostMediaResponse>> UploadPoster([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { code = "NO_FILE", message = "Không có file nào được gửi." });

            var userId = User.GetUserIdFromMetadata();

            var profile = await _profiles.GetEntityByIdAsync(userId);
            if (profile is null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });

            var media = await _profiles.UploadPosterAsync(userId, file);

            if (media is null)
                return BadRequest(new { code = "UPLOAD_FAILED", message = "Upload poster thất bại hoặc file không hợp lệ." });

            return Ok(media);
        }

        [HttpGet("recommendations")]
        [Authorize] // phải đăng nhập mới xem danh sách gợi ý
        public async Task<ActionResult<IEnumerable<ProfileResponse>>> GetRecommendations([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var viewerId = User.GetUserIdFromMetadata();

            // Nếu chưa có profile cho user hiện tại thì trả về rỗng / 404 tuỳ bạn chọn
            var viewerProfile = await _profiles.GetEntityByIdAsync(viewerId);
            if (viewerProfile is null)
            {
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ người dùng hiện tại không tồn tại." });
            }

            var items = await _profiles.GetRecommendedAsync(viewerId, skip, take);

            // (Tạm thời chưa filter thêm theo privacy; khi user click vào từng profile
            // thì GetById vẫn check privacy, nên vẫn an toàn.)
            return Ok(items);
        }

        // Lấy danh sách bạn bè đang online
        [HttpGet("online-friends")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProfileResponse>>> GetOnlineFriends([FromQuery] int withinLastMinutes = 15)
        {
            var userId = User.GetUserIdFromMetadata();

            // Nếu chưa có profile cho user hiện tại thì trả về rỗng
            var viewerProfile = await _profiles.GetEntityByIdAsync(userId);
            if (viewerProfile is null)
            {
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ người dùng hiện tại không tồn tại." });
            }

            var items = await _profiles.GetOnlineFriendsAsync(userId, withinLastMinutes);
            return Ok(items);
        }

        // Heartbeat endpoint để update LastActiveAt định kỳ
        [HttpPost("heartbeat")]
        [Authorize]
        public async Task<IActionResult> Heartbeat()
        {
            var userId = User.GetUserIdFromMetadata();

            // Check if profile exists
            var profile = await _profiles.GetEntityByIdAsync(userId);
            if (profile is null)
            {
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });
            }

            var lastActiveAt = await _profiles.UpdateLastActiveAsync(userId);

            return Ok(new { message = "Heartbeat recorded.", lastActiveAt });
        }
    }
}

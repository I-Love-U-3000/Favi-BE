using Favi_BE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilesSyncController : ControllerBase
    {
        private readonly IProfileService _profiles;

        public ProfilesSyncController(IProfileService profiles)
        {
            _profiles = profiles;
        }

        public record SupabaseUserCreatedDto(Guid user_id, string email, string? role, Dictionary<string, object>? raw_user_meta_data);

        [HttpPost("sync")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncProfile([FromBody] SupabaseUserCreatedDto dto)
        {
            try
            {
                // Check if profile already exists
                var existing = await _profiles.GetByIdAsync(dto.user_id);
                if (existing != null) return Ok("Profile already exists");

                // Ưu tiên lấy từ raw_user_meta_data nếu có
                var meta = dto.raw_user_meta_data ?? new Dictionary<string, object>();

                string username = meta.ContainsKey("username")
                    ? meta["username"]?.ToString() ?? dto.email.Split('@')[0]
                    : dto.email.Split('@')[0];

                string displayName = meta.ContainsKey("full_name")
                    ? meta["full_name"]?.ToString() ?? username
                    : username;

                await _profiles.CreateProfileAsync(dto.user_id, username, displayName);

                return Ok(new { message = "Profile created successfully", username, displayName });
            }
            catch (Exception ex)
            {
                return BadRequest($"Sync failed: {ex.Message}");
            }
        }

    }
}

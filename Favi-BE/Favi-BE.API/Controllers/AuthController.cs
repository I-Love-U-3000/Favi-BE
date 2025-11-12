using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISupabaseAuthService _supabase;
        private readonly IProfileService _profiles;

        public AuthController(ISupabaseAuthService supabase, IProfileService profiles)
        {
            _supabase = supabase;
            _profiles = profiles;
        }

        public record LoginDto(string Email, string Password);
        public record RegisterDto(string Email, string Password, string Username);

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<SupabaseAuthResponse>> Login(LoginDto dto)
        {
            var result = await _supabase.LoginAsync(dto.Email, dto.Password);
            if (result is null)
                return Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Email hoặc mật khẩu không đúng." });
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<ActionResult<SupabaseAuthResponse>> Register(RegisterDto dto)
        {
            var validate_result = await _profiles.CheckValidUsername(dto.Username);
            if (!validate_result)
                return Conflict(new { code = "USERNAME_EXISTS", message = "Username đã được sử dụng." });
            var result = await _supabase.RegisterAsync(dto.Email, dto.Password, dto.Username);
            if (result is null)
                return BadRequest(new { code = "REGISTRATION_FAILED", message = "Không thể tạo tài khoản. Vui lòng thử lại." });
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<SupabaseAuthResponse>> Refresh([FromBody] string refreshToken)
        {
            var result = await _supabase.RefreshAsync(refreshToken);
            if (result is null)
                return Unauthorized(new { code = "INVALID_REFRESH_TOKEN", message = "Refresh token không hợp lệ hoặc đã hết hạn." });
            return Ok(result);
        }
    }
}

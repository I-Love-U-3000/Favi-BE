using Favi_BE.Interfaces;
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

        public AuthController(ISupabaseAuthService supabase)
        {
            _supabase = supabase;
        }

        public record LoginDto(string Email, string Password);
        public record RegisterDto(string Email, string Password, string Username);

        [HttpPost("login")]
        public async Task<ActionResult<SupabaseAuthResponse>> Login(LoginDto dto)
        {
            var result = await _supabase.LoginAsync(dto.Email, dto.Password);
            if (result is null) return Unauthorized("Invalid credentials");
            return Ok(result); // trả thẳng access_token + refresh_token Supabase
        }

        [HttpPost("register")]
        public async Task<ActionResult<SupabaseAuthResponse>> Register(RegisterDto dto)
        {
            var result = await _supabase.RegisterAsync(dto.Email, dto.Password, dto.Username);
            if (result is null) return BadRequest("Registration failed");
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<SupabaseAuthResponse>> Refresh([FromBody] string refreshToken)
        {
            var result = await _supabase.RefreshAsync(refreshToken);
            if (result is null) return Unauthorized("Invalid refresh token");
            return Ok(result);
        }
    }
}

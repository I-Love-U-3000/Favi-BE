using Favi_BE.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwt;

        public record LoginDto(string Username, string Password);
        public record LoginResponse(string AccessToken, string RefreshToken, string Message);
        public record RefreshDto(string RefreshToken);
        public record RefreshResult(string AccessToken);

        public AuthController(IJwtService jwt) => _jwt = jwt;

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginDto dto)
        {
            // TODO: thay bằng check DB + hash password
            var user = FakeValidate(dto.Username, dto.Password);
            if (user is null) return BadRequest("Invalid credential");

            if (user.HasValue)
            {
                var access = _jwt.CreateAccessToken(user.Value.Id, user.Value.Role);
                var refresh = _jwt.CreateRefreshToken(user.Value.Id, user.Value.Role);
                return Ok(new LoginResponse(access, refresh, "Access granted successfully"));
            }
            else
            {
                return BadRequest("Invalid credential");
            }
        }

        [HttpPost("refresh")]
        public ActionResult<RefreshResult> Refresh([FromBody] RefreshDto dto)
        {
            var principal = _jwt.ValidateRefresh(dto.RefreshToken);
            if (principal is null) return BadRequest("Invalid refresh token");

            var sub = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            var role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "USER";
            if (sub is null) return BadRequest("Invalid token");

            var access = _jwt.CreateAccessToken(int.Parse(sub), role);
            return Ok(new RefreshResult(access));
        }

        private static (int Id, string Role)? FakeValidate(string u, string p)
            => u == "admin" && p == "admin" ? (1, "ADMIN") : null;
    }
}

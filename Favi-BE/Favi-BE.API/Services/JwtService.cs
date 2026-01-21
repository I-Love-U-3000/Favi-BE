using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Favi_BE.Common;

namespace Favi_BE.Services
{
    public interface IJwtService
    {
        string CreateAccessToken(Guid profileId, string username, string role);
        string CreateRefreshToken(Guid profileId, string username, string role);
        ClaimsPrincipal? ValidateRefresh(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly JwtOptions _opt;
        private readonly SigningCredentials _cred;
        private readonly TokenValidationParameters _baseValidation;

        public JwtService(IOptions<JwtOptions> opt)
        {
            _opt = opt.Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
            _cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            _baseValidation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _opt.Issuer,
                ValidateAudience = true,
                ValidAudience = _opt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role
            };
        }

        public string CreateAccessToken(Guid profileId, string username, string role)
            => CreateToken(profileId, username, role, "access", TimeSpan.FromMinutes(_opt.AccessMinutes));

        public string CreateRefreshToken(Guid profileId, string username, string role)
            => CreateToken(profileId, username, role, "refresh", TimeSpan.FromDays(_opt.RefreshDays));

        private string CreateToken(Guid profileId, string username, string role, string type, TimeSpan ttl)
        {
            var now = DateTime.UtcNow;
            var jti = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, profileId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.NameIdentifier, profileId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("type", type)
            };

            var jwt = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(ttl),
                signingCredentials: _cred
            );
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public ClaimsPrincipal? ValidateRefresh(string token)
        {
            var tvp = _baseValidation.Clone();
            tvp.TokenDecryptionKey = null;
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, tvp, out var _);
            // giống Spring: yêu cầu claim "type" = refresh  :contentReference[oaicite:6]{index=6}
            if (principal.FindFirst("type")?.Value != "refresh") return null;
            return principal;
        }
    }
}

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Favi_BE.Options;

namespace Favi_BE.Auth
{
    public interface IJwtService
    {
        string CreateAccessToken(int accountId, string role);
        string CreateRefreshToken(int accountId, string role);
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

        public string CreateAccessToken(int accountId, string role)
            => CreateToken(accountId, role, "access", TimeSpan.FromMinutes(_opt.AccessMinutes));

        public string CreateRefreshToken(int accountId, string role)
            => CreateToken(accountId, role, "refresh", TimeSpan.FromDays(_opt.RefreshDays));

        private string CreateToken(int accountId, string role, string type, TimeSpan ttl)
        {
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: new[] {
                new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("type", type)
                },
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

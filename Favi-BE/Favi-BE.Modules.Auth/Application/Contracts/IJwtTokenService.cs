using System.Security.Claims;

namespace Favi_BE.Modules.Auth.Application.Contracts;

/// <summary>
/// Token generation/validation port for the Auth module.
/// Implemented in Favi-BE.API by an adapter over the existing JwtService.
/// </summary>
public interface IJwtTokenService
{
    string CreateAccessToken(Guid profileId, string username, string role);
    string CreateRefreshToken(Guid profileId, string username, string role);
    ClaimsPrincipal? ValidateRefreshToken(string token);
}

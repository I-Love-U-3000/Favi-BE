using System.Security.Claims;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Services;

namespace Favi_BE.API.Application.Auth;

/// <summary>
/// Implements IJwtTokenService (Auth module port) by delegating to the existing IJwtService.
/// </summary>
internal sealed class JwtTokenServiceAdapter : IJwtTokenService
{
    private readonly IJwtService _jwtService;

    public JwtTokenServiceAdapter(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public string CreateAccessToken(Guid profileId, string username, string role)
        => _jwtService.CreateAccessToken(profileId, username, role);

    public string CreateRefreshToken(Guid profileId, string username, string role)
        => _jwtService.CreateRefreshToken(profileId, username, role);

    public ClaimsPrincipal? ValidateRefreshToken(string token)
        => _jwtService.ValidateRefresh(token);
}

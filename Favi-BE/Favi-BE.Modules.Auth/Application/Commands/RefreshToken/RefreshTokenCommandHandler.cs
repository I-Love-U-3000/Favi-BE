using System.IdentityModel.Tokens.Jwt;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthCommandResult>
{
    private readonly IAuthWriteRepository _authRepo;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenCommandHandler(IAuthWriteRepository authRepo, IJwtTokenService jwt)
    {
        _authRepo = authRepo;
        _jwt = jwt;
    }

    public async Task<AuthCommandResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwt.ValidateRefreshToken(request.RefreshToken);
        if (principal is null)
            return AuthCommandResult.Fail("INVALID_REFRESH_TOKEN", "Refresh token không hợp lệ hoặc đã hết hạn.");

        var profileIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(profileIdClaim, out var profileId))
            return AuthCommandResult.Fail("INVALID_REFRESH_TOKEN", "Refresh token không hợp lệ hoặc đã hết hạn.");

        var user = await _authRepo.FindUserByIdAsync(profileId, cancellationToken);
        if (user is null || user.IsBanned)
            return AuthCommandResult.Fail("INVALID_REFRESH_TOKEN", "Refresh token không hợp lệ hoặc đã hết hạn.");

        var newAccessToken = _jwt.CreateAccessToken(user.Id, user.Username, user.Role);

        // Refresh token rotation deferred to AuthSession slice; return same token for now.
        return AuthCommandResult.Success(newAccessToken, request.RefreshToken, "Token refreshed");
    }
}

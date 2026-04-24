using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.Login;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthCommandResult>
{
    private readonly IAuthWriteRepository _authRepo;
    private readonly IJwtTokenService _jwt;

    public LoginCommandHandler(IAuthWriteRepository authRepo, IJwtTokenService jwt)
    {
        _authRepo = authRepo;
        _jwt = jwt;
    }

    public async Task<AuthCommandResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Look up by email or username
        (AuthUserData? user, AuthCredentialData? credential) lookup;

        if (request.EmailOrUsername.Contains('@'))
            lookup = await _authRepo.FindByEmailAsync(request.EmailOrUsername, cancellationToken);
        else
            lookup = await _authRepo.FindByUsernameAsync(request.EmailOrUsername, cancellationToken);

        if (lookup.user is null || lookup.credential is null)
            return AuthCommandResult.Fail("INVALID_CREDENTIALS", "Email/username hoặc mật khẩu không đúng.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, lookup.credential.PasswordHash))
            return AuthCommandResult.Fail("INVALID_CREDENTIALS", "Email/username hoặc mật khẩu không đúng.");

        if (lookup.user.IsBanned)
            return AuthCommandResult.Fail("USER_BANNED", "Tài khoản đã bị khoá.");

        var accessToken = _jwt.CreateAccessToken(lookup.user.Id, lookup.user.Username, lookup.user.Role);
        var refreshToken = _jwt.CreateRefreshToken(lookup.user.Id, lookup.user.Username, lookup.user.Role);

        return AuthCommandResult.Success(accessToken, refreshToken, "Login successful");
    }
}

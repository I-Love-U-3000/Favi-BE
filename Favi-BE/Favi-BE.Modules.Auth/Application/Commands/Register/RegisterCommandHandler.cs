using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.Register;

internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthCommandResult>
{
    private readonly IAuthWriteRepository _authRepo;
    private readonly IJwtTokenService _jwt;

    public RegisterCommandHandler(IAuthWriteRepository authRepo, IJwtTokenService jwt)
    {
        _authRepo = authRepo;
        _jwt = jwt;
    }

    public async Task<AuthCommandResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Validate uniqueness — same order as legacy controller/service
        if (!await _authRepo.IsUsernameUniqueAsync(request.Username, cancellationToken))
            return AuthCommandResult.Fail("USERNAME_EXISTS", "Username đã được sử dụng.");

        if (!await _authRepo.IsEmailUniqueAsync(request.Email, cancellationToken))
            return AuthCommandResult.Fail("EMAIL_EXISTS", "Email đã được đăng ký.");

        var profileId = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var registrationData = new AuthUserRegistrationData(
            Id: profileId,
            Username: request.Username,
            DisplayName: request.DisplayName ?? request.Username,
            Email: request.Email,
            PasswordHash: passwordHash);

        await _authRepo.RegisterUserAsync(registrationData, cancellationToken);
        await _authRepo.SaveAsync(cancellationToken);

        var accessToken = _jwt.CreateAccessToken(profileId, request.Username, "user");
        var refreshToken = _jwt.CreateRefreshToken(profileId, request.Username, "user");

        return AuthCommandResult.Success(accessToken, refreshToken, "Registration successful");
    }
}

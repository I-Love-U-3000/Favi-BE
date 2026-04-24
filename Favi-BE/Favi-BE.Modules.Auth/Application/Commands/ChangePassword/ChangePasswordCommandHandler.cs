using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthCommandResult>
{
    private readonly IAuthWriteRepository _authRepo;

    public ChangePasswordCommandHandler(IAuthWriteRepository authRepo)
    {
        _authRepo = authRepo;
    }

    public async Task<AuthCommandResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var credential = await _authRepo.FindCredentialByProfileIdAsync(request.ProfileId, cancellationToken);
        if (credential is null)
            return AuthCommandResult.Fail("ACCOUNT_NOT_FOUND", "Tài khoản không tồn tại.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, credential.PasswordHash))
            return AuthCommandResult.Fail("WRONG_PASSWORD", "Mật khẩu hiện tại không đúng.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _authRepo.UpdatePasswordHashAsync(request.ProfileId, newHash, cancellationToken);
        await _authRepo.SaveAsync(cancellationToken);

        return AuthCommandResult.OkNoTokens("Đổi mật khẩu thành công.");
    }
}

using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.Logout;

/// <summary>
/// Logout handler.
/// Phase 1: client-side token discard only (no server-side revocation).
/// Phase 2 (additive): persist AuthSession revocation when AuthSession table is added.
/// </summary>
internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, AuthCommandResult>
{
    public Task<AuthCommandResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // AuthSession revocation is deferred until the AuthSession additive migration.
        // Client is responsible for discarding tokens after this call.
        return Task.FromResult(AuthCommandResult.OkNoTokens("Đăng xuất thành công."));
    }
}

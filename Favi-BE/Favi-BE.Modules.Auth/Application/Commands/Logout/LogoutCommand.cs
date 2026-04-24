using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Commands.Logout;

/// <summary>
/// Revokes the current session/refresh token.
/// No-op for now (no AuthSession table yet — client discards token).
/// Full session revocation will be wired in the AuthSession additive migration.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : ICommand<AuthCommandResult>;

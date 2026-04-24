using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Commands.RefreshToken;

/// <summary>
/// Issues a new access token using a valid refresh token.
/// Returns new access token (same refresh token) on success;
/// Error with code INVALID_REFRESH_TOKEN on failure.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthCommandResult>;

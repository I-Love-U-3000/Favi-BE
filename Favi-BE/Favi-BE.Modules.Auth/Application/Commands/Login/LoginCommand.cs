using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Commands.Login;

/// <summary>
/// Authenticates a user by email or username + password.
/// Returns auth tokens on success; Error with code INVALID_CREDENTIALS or USER_BANNED on failure.
/// </summary>
public sealed record LoginCommand(
    string EmailOrUsername,
    string Password) : ICommand<AuthCommandResult>;

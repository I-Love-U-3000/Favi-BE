using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Commands.Register;

/// <summary>
/// Registers a new user account (local auth).
/// Creates Profile + EmailAccount in one atomic transaction.
/// Returns auth tokens on success; Error on email/username conflict or failure.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string Username,
    string? DisplayName) : ICommand<AuthCommandResult>;

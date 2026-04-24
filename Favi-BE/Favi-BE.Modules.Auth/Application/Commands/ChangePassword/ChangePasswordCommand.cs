using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Commands.ChangePassword;

/// <summary>
/// Changes the authenticated user's password.
/// Verifies current password before persisting the new hash.
/// Returns error WRONG_PASSWORD if current password is incorrect.
/// </summary>
public sealed record ChangePasswordCommand(
    Guid ProfileId,
    string CurrentPassword,
    string NewPassword) : ICommand<AuthCommandResult>;

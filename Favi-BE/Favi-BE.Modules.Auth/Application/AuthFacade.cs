using Favi_BE.Modules.Auth.Application.Commands.ChangePassword;
using Favi_BE.Modules.Auth.Application.Commands.Login;
using Favi_BE.Modules.Auth.Application.Commands.Logout;
using Favi_BE.Modules.Auth.Application.Commands.RefreshToken;
using Favi_BE.Modules.Auth.Application.Commands.Register;
using Favi_BE.Modules.Auth.Application.Queries.GetCurrentUser;
using Favi_BE.Modules.Auth.Application.Queries.GetOnlineFriends;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileAvatar;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileById;
using Favi_BE.Modules.Auth.Application.Queries.GetProfilePoster;
using Favi_BE.Modules.Auth.Application.Queries.GetRecommendedProfiles;
using Favi_BE.Modules.Auth.Application.Commands.DeleteProfile;
using Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;
using Favi_BE.Modules.Auth.Application.Commands.UpdateProfile;
using Favi_BE.Modules.Auth.Application.Commands.UploadAvatar;
using Favi_BE.Modules.Auth.Application.Commands.UploadPoster;
using Favi_BE.Modules.Auth.Application.Responses;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Auth.Application;

public sealed class AuthFacade : IAuthFacade
{
    private readonly IMediator _mediator;

    public AuthFacade(IMediator mediator) => _mediator = mediator;

    public Task<AuthCommandResult> LoginAsync(LoginCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<AuthCommandResult> RegisterAsync(RegisterCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<AuthCommandResult> RefreshTokenAsync(RefreshTokenCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<AuthCommandResult> LogoutAsync(LogoutCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<AuthCommandResult> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<CurrentUserDto?> GetCurrentUserAsync(GetCurrentUserQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<ProfileReadModel?> GetProfileByIdAsync(GetProfileByIdQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<ProfileCommandResult> UpdateProfileAsync(UpdateProfileCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<bool> DeleteProfileAsync(DeleteProfileCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<string?> GetProfileAvatarAsync(GetProfileAvatarQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<string?> GetProfilePosterAsync(GetProfilePosterQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<SavedImageResult> UploadAvatarAsync(UploadAvatarCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<SavedImageResult> UploadPosterAsync(UploadPosterCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<IReadOnlyList<ProfileReadModel>> GetRecommendedProfilesAsync(GetRecommendedProfilesQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<IReadOnlyList<ProfileReadModel>> GetOnlineFriendsAsync(GetOnlineFriendsQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<DateTime> UpdateLastActiveAsync(UpdateLastActiveCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);
}

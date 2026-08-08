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
using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Auth.Application.Contracts;

public interface IAuthFacade
{
    Task<AuthCommandResult> LoginAsync(LoginCommand command, CancellationToken ct = default);
    Task<AuthCommandResult> RegisterAsync(RegisterCommand command, CancellationToken ct = default);
    Task<AuthCommandResult> RefreshTokenAsync(RefreshTokenCommand command, CancellationToken ct = default);
    Task<AuthCommandResult> LogoutAsync(LogoutCommand command, CancellationToken ct = default);
    Task<AuthCommandResult> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken ct = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(GetCurrentUserQuery query, CancellationToken ct = default);
    Task<ProfileReadModel?> GetProfileByIdAsync(GetProfileByIdQuery query, CancellationToken ct = default);
    Task<ProfileCommandResult> UpdateProfileAsync(UpdateProfileCommand command, CancellationToken ct = default);
    Task<bool> DeleteProfileAsync(DeleteProfileCommand command, CancellationToken ct = default);
    Task<string?> GetProfileAvatarAsync(GetProfileAvatarQuery query, CancellationToken ct = default);
    Task<string?> GetProfilePosterAsync(GetProfilePosterQuery query, CancellationToken ct = default);
    Task<SavedImageResult> UploadAvatarAsync(UploadAvatarCommand command, CancellationToken ct = default);
    Task<SavedImageResult> UploadPosterAsync(UploadPosterCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<ProfileReadModel>> GetRecommendedProfilesAsync(GetRecommendedProfilesQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<ProfileReadModel>> GetOnlineFriendsAsync(GetOnlineFriendsQuery query, CancellationToken ct = default);
    Task<DateTime> UpdateLastActiveAsync(UpdateLastActiveCommand command, CancellationToken ct = default);
}

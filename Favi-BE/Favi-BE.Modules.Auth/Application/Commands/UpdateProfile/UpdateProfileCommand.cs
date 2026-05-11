using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid ProfileId,
    string? Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    int? PrivacyLevel,
    int? FollowPrivacyLevel
) : IRequest<ProfileCommandResult>;

using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using Favi_BE.Modules.Auth.Domain;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileCommandResult>
{
    private readonly IAuthWriteRepository _repo;
    private readonly IAuthQueryReader _reader;

    public UpdateProfileCommandHandler(IAuthWriteRepository repo, IAuthQueryReader reader)
    {
        _repo = repo;
        _reader = reader;
    }

    public async Task<ProfileCommandResult> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var authUser = await _repo.FindUserByIdAsync(request.ProfileId, cancellationToken);
        if (authUser is null)
            return ProfileCommandResult.Fail("PROFILE_NOT_FOUND", "Không tìm thấy hồ sơ để cập nhật.");

        // Reconstitute the domain aggregate from storage details
        var profileAggregate = new Auth.Domain.Profile(
            authUser.Id,
            authUser.Username,
            authUser.DisplayName,
            authUser.AvatarUrl,
            coverUrl: null,
            bio: null,
            authUser.Role,
            DateTime.UtcNow,
            lastActiveAt: null,
            privacyLevel: request.PrivacyLevel?.ToString() ?? "0",
            followPrivacyLevel: request.FollowPrivacyLevel?.ToString() ?? "0",
            isBanned: authUser.IsBanned,
            bannedUntil: null
        );

        // Execute state transition on the aggregate root
        profileAggregate.Update(
            request.DisplayName,
            request.Bio,
            profileAggregate.PrivacyLevel,
            profileAggregate.FollowPrivacyLevel
        );

        var updated = await _repo.UpdateProfileAsync(
            profileAggregate.Id,
            request.Username,
            profileAggregate.DisplayName,
            profileAggregate.Bio,
            request.AvatarUrl,
            request.CoverUrl,
            int.Parse(profileAggregate.PrivacyLevel),
            int.Parse(profileAggregate.FollowPrivacyLevel),
            cancellationToken);

        if (!updated)
            return ProfileCommandResult.Fail("PROFILE_NOT_FOUND", "Không tìm thấy hồ sơ để cập nhật.");

        await _repo.SaveAsync(cancellationToken);

        var profile = await _reader.GetProfileByIdAsync(request.ProfileId, request.ProfileId, cancellationToken);
        return profile is null
            ? ProfileCommandResult.Fail("PROFILE_NOT_FOUND", "Không tìm thấy hồ sơ sau khi cập nhật.")
            : ProfileCommandResult.WithProfile(profile);
    }
}

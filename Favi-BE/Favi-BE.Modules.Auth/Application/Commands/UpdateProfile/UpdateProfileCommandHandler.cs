using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
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
        var updated = await _repo.UpdateProfileAsync(
            request.ProfileId,
            request.Username,
            request.DisplayName,
            request.Bio,
            request.AvatarUrl,
            request.CoverUrl,
            request.PrivacyLevel,
            request.FollowPrivacyLevel,
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

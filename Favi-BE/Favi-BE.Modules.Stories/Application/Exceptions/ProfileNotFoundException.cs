namespace Favi_BE.Modules.Stories.Application.Exceptions;

public sealed class ProfileNotFoundException : Exception
{
    public Guid ProfileId { get; }

    public ProfileNotFoundException(Guid profileId)
        : base($"Profile '{profileId}' not found.")
    {
        ProfileId = profileId;
    }
}

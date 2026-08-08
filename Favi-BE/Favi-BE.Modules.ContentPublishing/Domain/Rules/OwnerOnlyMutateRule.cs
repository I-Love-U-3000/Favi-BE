using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.ContentPublishing.Domain.Rules;

public sealed class OwnerOnlyMutateRule : IBusinessRule
{
    private readonly Guid _profileId;
    private readonly Guid _ownerId;

    public OwnerOnlyMutateRule(Guid profileId, Guid ownerId)
    {
        _profileId = profileId;
        _ownerId = ownerId;
    }

    public bool IsBroken() => _profileId != _ownerId;

    public string Message => "You are not authorized to perform actions on this content.";
}

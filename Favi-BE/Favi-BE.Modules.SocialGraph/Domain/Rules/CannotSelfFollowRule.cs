using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.SocialGraph.Domain.Rules;

public sealed class CannotSelfFollowRule : IBusinessRule
{
    private readonly Guid _followerId;
    private readonly Guid _followeeId;

    public CannotSelfFollowRule(Guid followerId, Guid followeeId)
    {
        _followerId = followerId;
        _followeeId = followeeId;
    }

    public bool IsBroken() => _followerId == _followeeId;

    public string Message => "You cannot follow your own profile.";
}

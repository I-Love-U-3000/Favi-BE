using Favi_BE.BuildingBlocks.Domain;
using Favi_BE.Modules.SocialGraph.Domain.Rules;

namespace Favi_BE.Modules.SocialGraph.Domain;

public sealed class FollowRelationship : Entity
{
    public Guid FollowerId { get; private set; }
    public Guid FolloweeId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public FollowRelationship(Guid followerId, Guid followeeId, DateTime createdAt)
    {
        FollowerId = followerId;
        FolloweeId = followeeId;
        CreatedAt = createdAt;
    }

    public static FollowRelationship Create(Guid followerId, Guid followeeId)
    {
        CheckRule(new CannotSelfFollowRule(followerId, followeeId));
        return new FollowRelationship(followerId, followeeId, DateTime.UtcNow);
    }
}

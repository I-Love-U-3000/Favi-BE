namespace Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels;

public sealed record FollowWriteData(
    Guid FollowerId,
    Guid FolloweeId,
    DateTime CreatedAt);

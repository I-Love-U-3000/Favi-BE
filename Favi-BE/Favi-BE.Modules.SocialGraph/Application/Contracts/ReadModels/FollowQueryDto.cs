namespace Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

public sealed record FollowQueryDto(
    Guid FollowerId,
    Guid FolloweeId,
    DateTime CreatedAt,
    string? Username = null,
    string? DisplayName = null,
    string? AvatarUrl = null,
    string? Bio = null);

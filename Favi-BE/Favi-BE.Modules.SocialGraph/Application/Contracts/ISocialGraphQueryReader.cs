using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.SocialGraph.Application.Contracts;

public interface ISocialGraphQueryReader
{
    Task<(IReadOnlyList<FollowQueryDto> Items, int TotalCount)> GetFollowersAsync(Guid profileId, int skip, int take, string? query = null, CancellationToken ct = default);
    Task<(IReadOnlyList<FollowQueryDto> Items, int TotalCount)> GetFollowingsAsync(Guid profileId, int skip, int take, string? query = null, CancellationToken ct = default);
    Task<IReadOnlyList<SocialLinkQueryDto>> GetSocialLinksAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>Cross-context lookup: returns true if the profile exists in the identity store.</summary>
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);
}

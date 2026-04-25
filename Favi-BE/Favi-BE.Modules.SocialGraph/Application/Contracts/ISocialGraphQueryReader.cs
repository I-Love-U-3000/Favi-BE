using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.SocialGraph.Application.Contracts;

public interface ISocialGraphQueryReader
{
    Task<IReadOnlyList<FollowQueryDto>> GetFollowersAsync(Guid profileId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<FollowQueryDto>> GetFollowingsAsync(Guid profileId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<SocialLinkQueryDto>> GetSocialLinksAsync(Guid profileId, CancellationToken ct = default);
}

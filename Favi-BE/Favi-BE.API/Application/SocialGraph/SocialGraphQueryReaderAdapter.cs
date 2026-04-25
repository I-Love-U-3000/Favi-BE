using Favi_BE.Interfaces;
using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;
using Favi_BE.Modules.SocialGraph.Domain;
using LegacySocialKind = Favi_BE.Models.Enums.SocialKind;

namespace Favi_BE.API.Application.SocialGraph;

internal sealed class SocialGraphQueryReaderAdapter : ISocialGraphQueryReader
{
    private readonly IUnitOfWork _uow;

    public SocialGraphQueryReaderAdapter(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<FollowQueryDto>> GetFollowersAsync(
        Guid profileId, int skip, int take, CancellationToken ct = default)
    {
        var follows = await _uow.Follows.GetFollowersAsync(profileId, skip, take);
        return follows.Select(f => new FollowQueryDto(f.FollowerId, f.FolloweeId, f.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<FollowQueryDto>> GetFollowingsAsync(
        Guid profileId, int skip, int take, CancellationToken ct = default)
    {
        var follows = await _uow.Follows.GetFollowingAsync(profileId, skip, take);
        return follows.Select(f => new FollowQueryDto(f.FollowerId, f.FolloweeId, f.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<SocialLinkQueryDto>> GetSocialLinksAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var links = await _uow.SocialLinks.GetByProfileIdAsync(profileId);
        return links.Select(l => new SocialLinkQueryDto(
            l.Id, l.ProfileId, MapSocialKind(l.Kind), l.Url, l.CreatedAt)).ToList();
    }

    private static SocialKind MapSocialKind(LegacySocialKind k) => (SocialKind)(int)k;
}

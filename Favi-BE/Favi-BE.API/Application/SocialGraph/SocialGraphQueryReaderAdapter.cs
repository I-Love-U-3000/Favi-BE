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

    public async Task<(IReadOnlyList<FollowQueryDto> Items, int TotalCount)> GetFollowersAsync(
        Guid profileId, int skip, int take, string? query = null, CancellationToken ct = default)
    {
        var follows = await _uow.Follows.GetFollowersAsync(profileId, skip, take, query);
        var count = await _uow.Follows.GetFollowersCountAsync(profileId, query);
        var dtos = follows.Select(f => new FollowQueryDto(
            f.FollowerId,
            f.FolloweeId,
            f.CreatedAt,
            f.Follower?.Username,
            f.Follower?.DisplayName,
            f.Follower?.AvatarUrl,
            f.Follower?.Bio)).ToList();
        return (dtos, count);
    }

    public async Task<(IReadOnlyList<FollowQueryDto> Items, int TotalCount)> GetFollowingsAsync(
        Guid profileId, int skip, int take, string? query = null, CancellationToken ct = default)
    {
        var follows = await _uow.Follows.GetFollowingAsync(profileId, skip, take, query);
        var count = await _uow.Follows.GetFollowingCountAsync(profileId, query);
        var dtos = follows.Select(f => new FollowQueryDto(
            f.FollowerId,
            f.FolloweeId,
            f.CreatedAt,
            f.Followee?.Username,
            f.Followee?.DisplayName,
            f.Followee?.AvatarUrl,
            f.Followee?.Bio)).ToList();
        return (dtos, count);
    }

    public async Task<IReadOnlyList<SocialLinkQueryDto>> GetSocialLinksAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var links = await _uow.SocialLinks.GetByProfileIdAsync(profileId);
        return links.Select(l => new SocialLinkQueryDto(
            l.Id, l.ProfileId, MapSocialKind(l.Kind), l.Url, l.CreatedAt)).ToList();
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    private static SocialKind MapSocialKind(LegacySocialKind k) => (SocialKind)(int)k;
}

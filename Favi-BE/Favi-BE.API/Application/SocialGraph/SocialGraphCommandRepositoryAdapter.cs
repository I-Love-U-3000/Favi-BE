using Favi_BE.Interfaces;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels;
using Favi_BE.Modules.SocialGraph.Domain;
using LegacySocialKind = Favi_BE.Models.Enums.SocialKind;

namespace Favi_BE.API.Application.SocialGraph;

internal sealed class SocialGraphCommandRepositoryAdapter : ISocialGraphCommandRepository
{
    private readonly IUnitOfWork _uow;

    public SocialGraphCommandRepositoryAdapter(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId, CancellationToken ct = default)
        => await _uow.Follows.IsFollowingAsync(followerId, followeeId);

    public async Task AddFollowAsync(FollowWriteData follow, CancellationToken ct = default)
        => await _uow.Follows.AddAsync(new Follow
        {
            FollowerId = follow.FollowerId,
            FolloweeId = follow.FolloweeId,
            CreatedAt = follow.CreatedAt
        });

    public async Task RemoveFollowAsync(Guid followerId, Guid followeeId, CancellationToken ct = default)
    {
        var follow = await _uow.Follows.GetAsync(followerId, followeeId);
        if (follow is not null)
            _uow.Follows.Remove(follow);
    }

    public async Task AddSocialLinkAsync(SocialLinkWriteData link, CancellationToken ct = default)
        => await _uow.SocialLinks.AddAsync(new SocialLink
        {
            Id = link.Id,
            ProfileId = link.ProfileId,
            Kind = MapSocialKind(link.Kind),
            Url = link.Url,
            CreatedAt = link.CreatedAt
        });

    public async Task<SocialLinkWriteData?> GetSocialLinkByIdAsync(Guid linkId, CancellationToken ct = default)
    {
        var link = await _uow.SocialLinks.GetByIdAsync(linkId);
        return link is null
            ? null
            : new SocialLinkWriteData(link.Id, link.ProfileId, MapSocialKind(link.Kind), link.Url, link.CreatedAt);
    }

    public async Task RemoveSocialLinkAsync(Guid linkId, CancellationToken ct = default)
    {
        var link = await _uow.SocialLinks.GetByIdAsync(linkId);
        if (link is not null)
            _uow.SocialLinks.Remove(link);
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    public async Task SaveAsync(CancellationToken ct = default)
        => await _uow.CompleteAsync();

    private static SocialKind MapSocialKind(LegacySocialKind k) => (SocialKind)(int)k;
    private static LegacySocialKind MapSocialKind(SocialKind k) => (LegacySocialKind)(int)k;
}

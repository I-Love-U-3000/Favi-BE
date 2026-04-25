using Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels;

namespace Favi_BE.Modules.SocialGraph.Application.Contracts;

public interface ISocialGraphCommandRepository
{
    // Follow write
    Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId, CancellationToken ct = default);
    Task AddFollowAsync(FollowWriteData follow, CancellationToken ct = default);
    Task RemoveFollowAsync(Guid followerId, Guid followeeId, CancellationToken ct = default);

    // SocialLink write
    Task AddSocialLinkAsync(SocialLinkWriteData link, CancellationToken ct = default);
    Task<SocialLinkWriteData?> GetSocialLinkByIdAsync(Guid linkId, CancellationToken ct = default);
    Task RemoveSocialLinkAsync(Guid linkId, CancellationToken ct = default);

    // Cross-context read lookup (for business rules)
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);
}

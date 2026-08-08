using Favi_BE.Modules.SocialGraph.Application.Commands.AddSocialLink;
using Favi_BE.Modules.SocialGraph.Application.Commands.FollowUser;
using Favi_BE.Modules.SocialGraph.Application.Commands.RemoveSocialLink;
using Favi_BE.Modules.SocialGraph.Application.Commands.UnfollowUser;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowers;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowings;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetSocialLinks;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.SocialGraph.Application.Contracts;

public interface ISocialGraphFacade
{
    Task<FollowCommandResult> FollowUserAsync(FollowUserCommand command, CancellationToken ct = default);
    Task<FollowCommandResult> UnfollowUserAsync(UnfollowUserCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<FollowQueryDto>> GetFollowersAsync(GetFollowersQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<FollowQueryDto>> GetFollowingsAsync(GetFollowingsQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<SocialLinkQueryDto>> GetSocialLinksAsync(GetSocialLinksQuery query, CancellationToken ct = default);
    Task<SocialLinkCommandResult> AddSocialLinkAsync(AddSocialLinkCommand command, CancellationToken ct = default);
    Task<SocialLinkCommandResult> RemoveSocialLinkAsync(RemoveSocialLinkCommand command, CancellationToken ct = default);
}

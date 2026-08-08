using Favi_BE.Modules.SocialGraph.Application.Commands.AddSocialLink;
using Favi_BE.Modules.SocialGraph.Application.Commands.FollowUser;
using Favi_BE.Modules.SocialGraph.Application.Commands.RemoveSocialLink;
using Favi_BE.Modules.SocialGraph.Application.Commands.UnfollowUser;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowers;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowings;
using Favi_BE.Modules.SocialGraph.Application.Queries.GetSocialLinks;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application;

public sealed class SocialGraphFacade : ISocialGraphFacade
{
    private readonly IMediator _mediator;

    public SocialGraphFacade(IMediator mediator) => _mediator = mediator;

    public Task<FollowCommandResult> FollowUserAsync(FollowUserCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<FollowCommandResult> UnfollowUserAsync(UnfollowUserCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<IReadOnlyList<FollowQueryDto>> GetFollowersAsync(GetFollowersQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<IReadOnlyList<FollowQueryDto>> GetFollowingsAsync(GetFollowingsQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<IReadOnlyList<SocialLinkQueryDto>> GetSocialLinksAsync(GetSocialLinksQuery query, CancellationToken ct)
        => _mediator.Send(query, ct);

    public Task<SocialLinkCommandResult> AddSocialLinkAsync(AddSocialLinkCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);

    public Task<SocialLinkCommandResult> RemoveSocialLinkAsync(RemoveSocialLinkCommand command, CancellationToken ct)
        => _mediator.Send(command, ct);
}

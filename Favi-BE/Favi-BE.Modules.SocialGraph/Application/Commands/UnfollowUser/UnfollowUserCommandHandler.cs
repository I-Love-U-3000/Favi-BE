using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Commands.UnfollowUser;

internal sealed class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand, FollowCommandResult>
{
    private readonly ISocialGraphCommandRepository _repo;

    public UnfollowUserCommandHandler(ISocialGraphCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<FollowCommandResult> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.IsFollowingAsync(request.FollowerId, request.FolloweeId, cancellationToken))
            return FollowCommandResult.Fail("NOT_FOLLOWING", "Bạn chưa theo dõi người dùng này.");

        await _repo.RemoveFollowAsync(request.FollowerId, request.FolloweeId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return FollowCommandResult.Success();
    }
}

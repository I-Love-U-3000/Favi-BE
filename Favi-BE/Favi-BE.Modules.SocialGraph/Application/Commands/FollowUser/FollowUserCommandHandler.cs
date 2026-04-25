using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Commands.FollowUser;

internal sealed class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, FollowCommandResult>
{
    private readonly ISocialGraphCommandRepository _repo;
    private readonly ISocialGraphNotificationService _notifications;

    public FollowUserCommandHandler(
        ISocialGraphCommandRepository repo,
        ISocialGraphNotificationService notifications)
    {
        _repo = repo;
        _notifications = notifications;
    }

    public async Task<FollowCommandResult> Handle(FollowUserCommand request, CancellationToken cancellationToken)
    {
        if (request.FollowerId == request.FolloweeId)
            return FollowCommandResult.Fail("SELF_FOLLOW", "Không thể tự theo dõi bản thân.");

        if (!await _repo.ProfileExistsAsync(request.FolloweeId, cancellationToken))
            return FollowCommandResult.Fail("PROFILE_NOT_FOUND", "Hồ sơ mục tiêu không tồn tại.");

        if (await _repo.IsFollowingAsync(request.FollowerId, request.FolloweeId, cancellationToken))
            return FollowCommandResult.Success();

        await _repo.AddFollowAsync(
            new FollowWriteData(request.FollowerId, request.FolloweeId, DateTime.UtcNow),
            cancellationToken);

        await _repo.SaveAsync(cancellationToken);

        await _notifications.NotifyUserFollowedAsync(request.FollowerId, request.FolloweeId, cancellationToken);

        return FollowCommandResult.Success();
    }
}

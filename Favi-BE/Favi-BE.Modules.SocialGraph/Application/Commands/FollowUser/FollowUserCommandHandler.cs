using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using Favi_BE.Modules.SocialGraph.Domain;
using Favi_BE.Modules.SocialGraph.Domain.Events;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Commands.FollowUser;

internal sealed class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, FollowCommandResult>
{
    private readonly ISocialGraphCommandRepository _repo;
    private readonly IDomainEventRegistry _domainEvents;

    public FollowUserCommandHandler(
        ISocialGraphCommandRepository repo,
        IDomainEventRegistry domainEvents)
    {
        _repo = repo;
        _domainEvents = domainEvents;
    }

    public async Task<FollowCommandResult> Handle(FollowUserCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.ProfileExistsAsync(request.FolloweeId, cancellationToken))
            return FollowCommandResult.Fail("PROFILE_NOT_FOUND", "Hồ sơ mục tiêu không tồn tại.");

        if (await _repo.IsFollowingAsync(request.FollowerId, request.FolloweeId, cancellationToken))
            return FollowCommandResult.Success();

        // Create the FollowRelationship aggregate root to check business rules (CannotSelfFollowRule)
        var relationship = FollowRelationship.Create(request.FollowerId, request.FolloweeId);

        await _repo.AddFollowAsync(
            new FollowWriteData(relationship.FollowerId, relationship.FolloweeId, relationship.CreatedAt),
            cancellationToken);

        await _repo.SaveAsync(cancellationToken);

        _domainEvents.Raise(new UserFollowedDomainEvent(request.FollowerId, request.FolloweeId, DateTime.UtcNow));

        return FollowCommandResult.Success();
    }
}

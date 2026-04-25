using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Favi_BE.Modules.Engagement.Application.Responses;
using Favi_BE.Modules.Engagement.Domain;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.TogglePostReaction;

internal sealed class TogglePostReactionCommandHandler : IRequestHandler<TogglePostReactionCommand, ReactionCommandResult>
{
    private readonly IEngagementCommandRepository _repo;
    private readonly IEngagementNotificationService _notifications;

    public TogglePostReactionCommandHandler(
        IEngagementCommandRepository repo,
        IEngagementNotificationService notifications)
    {
        _repo = repo;
        _notifications = notifications;
    }

    public async Task<ReactionCommandResult> Handle(TogglePostReactionCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.PostExistsAsync(request.PostId, cancellationToken))
            return ReactionCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        var existing = await _repo.GetPostReactionByActorAsync(request.ActorId, request.PostId, cancellationToken);

        if (existing is null)
        {
            await _repo.AddReactionAsync(new ReactionWriteData(
                Id: Guid.NewGuid(),
                ProfileId: request.ActorId,
                PostId: request.PostId,
                CommentId: null,
                CollectionId: null,
                RepostId: null,
                Type: request.Type,
                CreatedAt: DateTime.UtcNow), cancellationToken);

            await _repo.SaveAsync(cancellationToken);

            await _notifications.NotifyPostReactionAddedAsync(request.ActorId, request.PostId, cancellationToken);

            return ReactionCommandResult.Added(request.Type);
        }

        if (existing.Type == request.Type)
        {
            await _repo.RemoveReactionAsync(existing.Id, cancellationToken);
            await _repo.SaveAsync(cancellationToken);
            return ReactionCommandResult.RemovedReaction();
        }

        await _repo.UpdateReactionTypeAsync(existing.Id, request.Type, cancellationToken);
        await _repo.SaveAsync(cancellationToken);
        return ReactionCommandResult.Changed(request.Type);
    }
}

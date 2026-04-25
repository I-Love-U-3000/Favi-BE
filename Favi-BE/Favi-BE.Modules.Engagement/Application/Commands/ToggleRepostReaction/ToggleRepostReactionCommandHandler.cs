using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Favi_BE.Modules.Engagement.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.ToggleRepostReaction;

internal sealed class ToggleRepostReactionCommandHandler : IRequestHandler<ToggleRepostReactionCommand, ReactionCommandResult>
{
    private readonly IEngagementCommandRepository _repo;

    public ToggleRepostReactionCommandHandler(IEngagementCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<ReactionCommandResult> Handle(ToggleRepostReactionCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.RepostExistsAsync(request.RepostId, cancellationToken))
            return ReactionCommandResult.Fail("REPOST_NOT_FOUND", "Bài chia sẻ không tồn tại.");

        var existing = await _repo.GetRepostReactionByActorAsync(request.ActorId, request.RepostId, cancellationToken);

        if (existing is null)
        {
            await _repo.AddReactionAsync(new ReactionWriteData(
                Id: Guid.NewGuid(),
                ProfileId: request.ActorId,
                PostId: null,
                CommentId: null,
                CollectionId: null,
                RepostId: request.RepostId,
                Type: request.Type,
                CreatedAt: DateTime.UtcNow), cancellationToken);

            await _repo.SaveAsync(cancellationToken);
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

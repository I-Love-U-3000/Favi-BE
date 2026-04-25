using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Favi_BE.Modules.Engagement.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.ToggleCollectionReaction;

internal sealed class ToggleCollectionReactionCommandHandler : IRequestHandler<ToggleCollectionReactionCommand, ReactionCommandResult>
{
    private readonly IEngagementCommandRepository _repo;

    public ToggleCollectionReactionCommandHandler(IEngagementCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<ReactionCommandResult> Handle(ToggleCollectionReactionCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.CollectionExistsAsync(request.CollectionId, cancellationToken))
            return ReactionCommandResult.Fail("COLLECTION_NOT_FOUND", "Bộ sưu tập không tồn tại.");

        var existing = await _repo.GetCollectionReactionByActorAsync(request.ActorId, request.CollectionId, cancellationToken);

        if (existing is null)
        {
            await _repo.AddReactionAsync(new ReactionWriteData(
                Id: Guid.NewGuid(),
                ProfileId: request.ActorId,
                PostId: null,
                CommentId: null,
                CollectionId: request.CollectionId,
                RepostId: null,
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

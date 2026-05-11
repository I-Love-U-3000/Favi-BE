using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Favi_BE.Modules.Engagement.Application.Responses;
using Favi_BE.Modules.Engagement.Domain.Events;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.ToggleCommentReaction;

internal sealed class ToggleCommentReactionCommandHandler : IRequestHandler<ToggleCommentReactionCommand, ReactionCommandResult>
{
    private readonly IEngagementCommandRepository _repo;
    private readonly IDomainEventRegistry _domainEvents;

    public ToggleCommentReactionCommandHandler(
        IEngagementCommandRepository repo,
        IDomainEventRegistry domainEvents)
    {
        _repo = repo;
        _domainEvents = domainEvents;
    }

    public async Task<ReactionCommandResult> Handle(ToggleCommentReactionCommand request, CancellationToken cancellationToken)
    {
        var commentExists = await _repo.GetCommentByIdAsync(request.CommentId, cancellationToken);
        if (commentExists is null)
            return ReactionCommandResult.Fail("COMMENT_NOT_FOUND", "Bình luận không tồn tại.");

        var existing = await _repo.GetCommentReactionByActorAsync(request.ActorId, request.CommentId, cancellationToken);

        if (existing is null)
        {
            await _repo.AddReactionAsync(new ReactionWriteData(
                Id: Guid.NewGuid(),
                ProfileId: request.ActorId,
                PostId: null,
                CommentId: request.CommentId,
                CollectionId: null,
                RepostId: null,
                Type: request.Type,
                CreatedAt: DateTime.UtcNow), cancellationToken);

            await _repo.SaveAsync(cancellationToken);

            _domainEvents.Raise(new CommentReactionAddedDomainEvent(request.ActorId, request.CommentId, DateTime.UtcNow));

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

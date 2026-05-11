using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Favi_BE.Modules.Engagement.Application.Responses;
using Favi_BE.Modules.Engagement.Domain.Events;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.CreateComment;

internal sealed class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentCommandResult>
{
    private readonly IEngagementCommandRepository _repo;
    private readonly IDomainEventRegistry _domainEvents;

    public CreateCommentCommandHandler(
        IEngagementCommandRepository repo,
        IDomainEventRegistry domainEvents)
    {
        _repo = repo;
        _domainEvents = domainEvents;
    }

    public async Task<CommentCommandResult> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.PostExistsAsync(request.PostId, cancellationToken))
            return CommentCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        var commentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var commentData = new CommentWriteData(
            Id: commentId,
            PostId: request.PostId,
            RepostId: request.RepostId,
            ProfileId: request.AuthorId,
            Content: request.Content,
            MediaUrl: request.MediaUrl,
            ParentCommentId: request.ParentCommentId,
            CreatedAt: now,
            UpdatedAt: now);

        await _repo.AddCommentAsync(commentData, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        _domainEvents.Raise(new CommentCreatedDomainEvent(request.AuthorId, request.PostId, commentId, now));

        var emptyReactions = new ReactionSummaryQueryDto(0, new Dictionary<Domain.ReactionType, int>(), null);
        var dto = new CommentQueryDto(
            commentId, request.PostId, request.RepostId, request.AuthorId,
            request.Content, request.MediaUrl, request.ParentCommentId,
            now, now, emptyReactions);

        return CommentCommandResult.Success(dto);
    }
}

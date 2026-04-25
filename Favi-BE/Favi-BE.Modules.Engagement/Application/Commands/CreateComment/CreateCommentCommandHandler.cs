using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Favi_BE.Modules.Engagement.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.CreateComment;

internal sealed class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentCommandResult>
{
    private readonly IEngagementCommandRepository _repo;
    private readonly IEngagementNotificationService _notifications;

    public CreateCommentCommandHandler(
        IEngagementCommandRepository repo,
        IEngagementNotificationService notifications)
    {
        _repo = repo;
        _notifications = notifications;
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

        await _notifications.NotifyCommentCreatedAsync(request.AuthorId, request.PostId, commentId, cancellationToken);

        var emptyReactions = new ReactionSummaryQueryDto(0, new Dictionary<Domain.ReactionType, int>(), null);
        var dto = new CommentQueryDto(
            commentId, request.PostId, request.RepostId, request.AuthorId,
            request.Content, request.MediaUrl, request.ParentCommentId,
            now, now, emptyReactions);

        return CommentCommandResult.Success(dto);
    }
}

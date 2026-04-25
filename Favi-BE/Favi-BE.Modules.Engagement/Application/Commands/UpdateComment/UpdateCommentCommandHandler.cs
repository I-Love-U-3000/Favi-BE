using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Favi_BE.Modules.Engagement.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.UpdateComment;

internal sealed class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentCommandResult>
{
    private readonly IEngagementCommandRepository _repo;

    public UpdateCommentCommandHandler(IEngagementCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<CommentCommandResult> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetCommentByIdAsync(request.CommentId, cancellationToken);
        if (existing is null)
            return CommentCommandResult.Fail("COMMENT_NOT_FOUND", "Bình luận không tồn tại.");

        if (existing.ProfileId != request.RequesterId)
            return CommentCommandResult.Fail("COMMENT_FORBIDDEN", "Bạn không có quyền sửa bình luận này.");

        var updatedAt = DateTime.UtcNow;
        var updated = existing with { Content = request.Content, UpdatedAt = updatedAt };
        await _repo.UpdateCommentAsync(updated, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        // Build response from in-hand data — reactions are unchanged by an update
        var emptyReactions = new ReactionSummaryQueryDto(0, new Dictionary<Domain.ReactionType, int>(), null);
        var dto = new CommentQueryDto(
            updated.Id, updated.PostId, updated.RepostId, updated.ProfileId,
            updated.Content, updated.MediaUrl, updated.ParentCommentId,
            updated.CreatedAt, updatedAt, emptyReactions);

        return CommentCommandResult.Success(dto);
    }
}

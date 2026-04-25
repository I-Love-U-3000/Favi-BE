using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Engagement.Application.Commands.DeleteComment;

internal sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, EngagementCommandResult>
{
    private readonly IEngagementCommandRepository _repo;

    public DeleteCommentCommandHandler(IEngagementCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<EngagementCommandResult> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetCommentByIdAsync(request.CommentId, cancellationToken);
        if (existing is null)
            return EngagementCommandResult.Fail("COMMENT_NOT_FOUND_OR_FORBIDDEN", "Không tìm thấy bình luận hoặc bạn không có quyền xoá.");

        if (existing.ProfileId != request.RequesterId)
            return EngagementCommandResult.Fail("COMMENT_NOT_FOUND_OR_FORBIDDEN", "Không tìm thấy bình luận hoặc bạn không có quyền xoá.");

        await _repo.RemoveCommentAsync(request.CommentId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return EngagementCommandResult.Success();
    }
}

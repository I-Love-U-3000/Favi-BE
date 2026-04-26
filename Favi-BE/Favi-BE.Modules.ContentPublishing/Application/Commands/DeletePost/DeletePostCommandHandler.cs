using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.DeletePost;

internal sealed class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public DeletePostCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền xóa bài viết này.");

        // Soft delete: 30-day grace period before permanent removal
        await _repo.SoftDeletePostAsync(request.PostId, DateTime.UtcNow.AddDays(30), cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

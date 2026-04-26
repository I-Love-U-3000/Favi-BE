using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.ArchivePost;

internal sealed class ArchivePostCommandHandler : IRequestHandler<ArchivePostCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public ArchivePostCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(ArchivePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền lưu trữ bài viết này.");

        if (post.DeletedDayExpiredAt is not null)
            return PostCommandResult.Fail("POST_DELETED", "Không thể lưu trữ bài viết đã xóa.");

        await _repo.SetPostArchivedAsync(request.PostId, request.IsArchived, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

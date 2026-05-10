using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.PermanentDeletePost;

internal sealed class PermanentDeletePostCommandHandler : IRequestHandler<PermanentDeletePostCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public PermanentDeletePostCommandHandler(IContentPublishingCommandRepository repo) => _repo = repo;

    public async Task<PostCommandResult> Handle(PermanentDeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền xóa vĩnh viễn bài viết này.");

        await _repo.PermanentDeletePostAsync(request.PostId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

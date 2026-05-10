using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.RestorePost;

internal sealed class RestorePostCommandHandler : IRequestHandler<RestorePostCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public RestorePostCommandHandler(IContentPublishingCommandRepository repo) => _repo = repo;

    public async Task<PostCommandResult> Handle(RestorePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền khôi phục bài viết này.");

        if (post.DeletedDayExpiredAt is null)
            return PostCommandResult.Fail("POST_NOT_DELETED", "Bài viết chưa bị xóa.");

        await _repo.RestorePostAsync(request.PostId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.RemovePostTag;

internal sealed class RemovePostTagCommandHandler : IRequestHandler<RemovePostTagCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public RemovePostTagCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(RemovePostTagCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền xóa tag khỏi bài viết này.");

        await _repo.RemovePostTagAsync(request.PostId, request.TagId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

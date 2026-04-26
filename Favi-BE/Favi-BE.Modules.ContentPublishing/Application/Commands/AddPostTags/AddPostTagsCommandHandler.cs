using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostTags;

internal sealed class AddPostTagsCommandHandler : IRequestHandler<AddPostTagsCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public AddPostTagsCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(AddPostTagsCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền thêm tag vào bài viết này.");

        await _repo.AddPostTagsAsync(request.PostId, request.TagNames, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

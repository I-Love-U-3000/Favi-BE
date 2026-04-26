using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.UpdatePost;

internal sealed class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public UpdatePostCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền chỉnh sửa bài viết này.");

        var updated = post with
        {
            Caption = request.Caption is not null ? request.Caption.Trim() : post.Caption,
            Privacy = request.Privacy ?? post.Privacy,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdatePostAsync(updated, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

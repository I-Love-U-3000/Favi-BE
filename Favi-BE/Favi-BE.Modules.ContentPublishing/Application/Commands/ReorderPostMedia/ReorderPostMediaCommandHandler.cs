using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.ReorderPostMedia;

internal sealed class ReorderPostMediaCommandHandler : IRequestHandler<ReorderPostMediaCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public ReorderPostMediaCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(ReorderPostMediaCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền sắp xếp media của bài viết này.");

        await _repo.UpdatePostMediaPositionsAsync(request.PostId, request.Positions, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

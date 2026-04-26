using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostMedia;

internal sealed class AddPostMediaCommandHandler : IRequestHandler<AddPostMediaCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public AddPostMediaCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(AddPostMediaCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (post is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (post.ProfileId != request.RequesterId)
            return PostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền thêm media vào bài viết này.");

        var existing = await _repo.GetPostMediaAsync(request.PostId, cancellationToken);
        var nextPosition = existing.Count > 0 ? existing.Max(m => m.Position) + 1 : 0;

        var newItems = request.MediaItems
            .Select((m, i) => new PostMediaWriteData(
                Id: Guid.NewGuid(),
                PostId: request.PostId,
                Url: m.Url,
                ThumbnailUrl: m.ThumbnailUrl,
                PublicId: m.PublicId,
                Width: m.Width,
                Height: m.Height,
                Format: m.Format,
                Position: nextPosition + i
            ));

        await _repo.AddPostMediaRangeAsync(newItems, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

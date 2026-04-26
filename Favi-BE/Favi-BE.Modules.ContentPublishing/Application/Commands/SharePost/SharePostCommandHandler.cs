using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.SharePost;

internal sealed class SharePostCommandHandler : IRequestHandler<SharePostCommand, RepostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public SharePostCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<RepostCommandResult> Handle(SharePostCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.PostExistsAsync(request.OriginalPostId, cancellationToken))
            return RepostCommandResult.Fail("POST_NOT_FOUND", "Bài viết gốc không tồn tại.");

        if (!await _repo.CanProfileViewPostAsync(request.SharerId, request.OriginalPostId, cancellationToken))
            return RepostCommandResult.Fail("FORBIDDEN", "Bạn không có quyền chia sẻ bài viết này.");

        var existing = await _repo.GetRepostAsync(request.SharerId, request.OriginalPostId, cancellationToken);
        if (existing is not null)
            return RepostCommandResult.Ok(existing.Id);

        var now = DateTime.UtcNow;
        var repost = new RepostWriteData(
            Id: Guid.NewGuid(),
            ProfileId: request.SharerId,
            OriginalPostId: request.OriginalPostId,
            Caption: request.Caption,
            CreatedAt: now,
            UpdatedAt: now
        );

        await _repo.AddRepostAsync(repost, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return RepostCommandResult.Ok(repost.Id);
    }
}

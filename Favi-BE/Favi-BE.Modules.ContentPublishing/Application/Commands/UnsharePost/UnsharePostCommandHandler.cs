using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.UnsharePost;

internal sealed class UnsharePostCommandHandler : IRequestHandler<UnsharePostCommand, RepostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public UnsharePostCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<RepostCommandResult> Handle(UnsharePostCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetRepostAsync(request.SharerId, request.OriginalPostId, cancellationToken);
        if (existing is null)
            return RepostCommandResult.Fail("REPOST_NOT_FOUND", "Không tìm thấy repost để hủy.");

        await _repo.RemoveRepostAsync(request.SharerId, request.OriginalPostId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return RepostCommandResult.Ok();
    }
}

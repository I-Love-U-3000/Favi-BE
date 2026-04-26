using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.RemovePostFromCollection;

internal sealed class RemovePostFromCollectionCommandHandler : IRequestHandler<RemovePostFromCollectionCommand, CollectionCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public RemovePostFromCollectionCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<CollectionCommandResult> Handle(RemovePostFromCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _repo.GetCollectionForWriteAsync(request.CollectionId, cancellationToken);
        if (collection is null)
            return CollectionCommandResult.Fail("COLLECTION_NOT_FOUND", "Bộ sưu tập không tồn tại.");

        if (collection.ProfileId != request.RequesterId)
            return CollectionCommandResult.Fail("FORBIDDEN", "Bạn không có quyền xóa bài viết khỏi bộ sưu tập này.");

        await _repo.RemovePostFromCollectionAsync(request.CollectionId, request.PostId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return CollectionCommandResult.Ok(request.CollectionId);
    }
}

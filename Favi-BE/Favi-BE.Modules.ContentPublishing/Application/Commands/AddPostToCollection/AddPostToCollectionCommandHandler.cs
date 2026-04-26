using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostToCollection;

internal sealed class AddPostToCollectionCommandHandler : IRequestHandler<AddPostToCollectionCommand, CollectionCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public AddPostToCollectionCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<CollectionCommandResult> Handle(AddPostToCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _repo.GetCollectionForWriteAsync(request.CollectionId, cancellationToken);
        if (collection is null)
            return CollectionCommandResult.Fail("COLLECTION_NOT_FOUND", "Bộ sưu tập không tồn tại.");

        if (collection.ProfileId != request.RequesterId)
            return CollectionCommandResult.Fail("FORBIDDEN", "Bạn không có quyền thêm bài viết vào bộ sưu tập này.");

        if (!await _repo.PostExistsAsync(request.PostId, cancellationToken))
            return CollectionCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        if (await _repo.IsPostInCollectionAsync(request.CollectionId, request.PostId, cancellationToken))
            return CollectionCommandResult.Ok(request.CollectionId);

        await _repo.AddPostToCollectionAsync(request.CollectionId, request.PostId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return CollectionCommandResult.Ok(request.CollectionId);
    }
}

using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.DeleteCollection;

internal sealed class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand, CollectionCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public DeleteCollectionCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<CollectionCommandResult> Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _repo.GetCollectionForWriteAsync(request.CollectionId, cancellationToken);
        if (collection is null)
            return CollectionCommandResult.Fail("COLLECTION_NOT_FOUND", "Bộ sưu tập không tồn tại.");

        if (collection.ProfileId != request.RequesterId)
            return CollectionCommandResult.Fail("FORBIDDEN", "Bạn không có quyền xóa bộ sưu tập này.");

        await _repo.DeleteCollectionAsync(request.CollectionId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return CollectionCommandResult.Ok();
    }
}

using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.UpdateCollection;

internal sealed class UpdateCollectionCommandHandler : IRequestHandler<UpdateCollectionCommand, CollectionCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public UpdateCollectionCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<CollectionCommandResult> Handle(UpdateCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _repo.GetCollectionForWriteAsync(request.CollectionId, cancellationToken);
        if (collection is null)
            return CollectionCommandResult.Fail("COLLECTION_NOT_FOUND", "Bộ sưu tập không tồn tại.");

        if (collection.ProfileId != request.RequesterId)
            return CollectionCommandResult.Fail("FORBIDDEN", "Bạn không có quyền chỉnh sửa bộ sưu tập này.");

        var updated = collection with
        {
            Title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : collection.Title,
            Description = request.Description is not null ? request.Description.Trim() : collection.Description,
            Privacy = request.Privacy ?? collection.Privacy,
            CoverImageUrl = request.CoverImageUrl ?? collection.CoverImageUrl,
            CoverImagePublicId = request.CoverImagePublicId ?? collection.CoverImagePublicId,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdateCollectionAsync(updated, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return CollectionCommandResult.Ok(request.CollectionId);
    }
}

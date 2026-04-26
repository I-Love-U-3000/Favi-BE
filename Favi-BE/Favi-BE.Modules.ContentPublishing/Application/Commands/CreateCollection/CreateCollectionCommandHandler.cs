using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.CreateCollection;

internal sealed class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, CollectionCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public CreateCollectionCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<CollectionCommandResult> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var collectionId = Guid.NewGuid();

        var collection = new CollectionWriteData(
            Id: collectionId,
            ProfileId: request.OwnerId,
            Title: request.Title.Trim(),
            Description: request.Description?.Trim(),
            CoverImageUrl: request.CoverImageUrl,
            CoverImagePublicId: request.CoverImagePublicId,
            Privacy: request.Privacy,
            CreatedAt: now,
            UpdatedAt: now
        );

        await _repo.AddCollectionAsync(collection, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return CollectionCommandResult.Ok(collectionId);
    }
}

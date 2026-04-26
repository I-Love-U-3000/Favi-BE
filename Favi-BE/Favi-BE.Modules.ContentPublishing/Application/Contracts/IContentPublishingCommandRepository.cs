using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;

namespace Favi_BE.Modules.ContentPublishing.Application.Contracts;

public interface IContentPublishingCommandRepository
{
    // Post
    Task AddPostAsync(PostWriteData data, CancellationToken ct = default);
    Task<PostWriteData?> GetPostForWriteAsync(Guid postId, CancellationToken ct = default);
    Task UpdatePostAsync(PostWriteData data, CancellationToken ct = default);
    Task SoftDeletePostAsync(Guid postId, DateTime expiredAt, CancellationToken ct = default);
    Task SetPostArchivedAsync(Guid postId, bool isArchived, CancellationToken ct = default);

    // PostMedia
    Task AddPostMediaRangeAsync(IEnumerable<PostMediaWriteData> items, CancellationToken ct = default);
    Task<IReadOnlyList<PostMediaWriteData>> GetPostMediaAsync(Guid postId, CancellationToken ct = default);
    Task UpdatePostMediaPositionsAsync(Guid postId, IEnumerable<(Guid MediaId, int Position)> positions, CancellationToken ct = default);

    // PostTags (get-or-create tag by name, link to post)
    Task AddPostTagsAsync(Guid postId, IEnumerable<string> tagNames, CancellationToken ct = default);
    Task RemovePostTagAsync(Guid postId, Guid tagId, CancellationToken ct = default);

    // Collection
    Task AddCollectionAsync(CollectionWriteData data, CancellationToken ct = default);
    Task<CollectionWriteData?> GetCollectionForWriteAsync(Guid collectionId, CancellationToken ct = default);
    Task UpdateCollectionAsync(CollectionWriteData data, CancellationToken ct = default);
    Task DeleteCollectionAsync(Guid collectionId, CancellationToken ct = default);
    Task<bool> IsPostInCollectionAsync(Guid collectionId, Guid postId, CancellationToken ct = default);
    Task AddPostToCollectionAsync(Guid collectionId, Guid postId, CancellationToken ct = default);
    Task RemovePostFromCollectionAsync(Guid collectionId, Guid postId, CancellationToken ct = default);

    // Repost
    Task AddRepostAsync(RepostWriteData data, CancellationToken ct = default);
    Task<RepostWriteData?> GetRepostAsync(Guid sharerId, Guid originalPostId, CancellationToken ct = default);
    Task RemoveRepostAsync(Guid sharerId, Guid originalPostId, CancellationToken ct = default);

    // Cross-context existence checks for business rules
    Task<bool> PostExistsAsync(Guid postId, CancellationToken ct = default);
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);
    Task<bool> CanProfileViewPostAsync(Guid? viewerId, Guid postId, CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);
}

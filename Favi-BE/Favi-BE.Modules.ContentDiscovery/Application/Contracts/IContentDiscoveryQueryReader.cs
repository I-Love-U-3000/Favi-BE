using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts;

public interface IContentDiscoveryQueryReader
{
    // Post
    Task<PostReadModel?> GetPostByIdAsync(Guid postId, Guid? viewerId, CancellationToken ct = default);
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetProfilePostsAsync(Guid profileId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetNewsFeedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetGuestFeedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetExploreFeedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetLatestFeedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetArchivedPostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetRecycleBinAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> SearchPostsAsync(string query, Guid? userId, int page, int pageSize, CancellationToken ct = default);

    // Repost
    Task<RepostReadModel?> GetRepostByIdAsync(Guid repostId, Guid? viewerId, CancellationToken ct = default);
    Task<(IReadOnlyList<RepostReadModel> Items, int TotalCount)> GetRepostsByProfileAsync(Guid profileId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default);

    // Feed with reposts
    Task<(IReadOnlyList<FeedItemReadModel> Items, int TotalCount)> GetFeedWithRepostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    // Collection
    Task<CollectionReadModel?> GetCollectionByIdAsync(Guid collectionId, Guid? viewerId, CancellationToken ct = default);
    Task<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)> GetCollectionsByOwnerAsync(Guid ownerId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetCollectionPostsAsync(Guid collectionId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)> GetTrendingCollectionsAsync(Guid? viewerId, int page, int pageSize, CancellationToken ct = default);
}

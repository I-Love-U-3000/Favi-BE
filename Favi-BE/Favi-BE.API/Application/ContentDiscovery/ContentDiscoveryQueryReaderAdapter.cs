using Favi_BE.API.Models.Entities;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using LegacyPrivacy = Favi_BE.Models.Enums.PrivacyLevel;

namespace Favi_BE.API.Application.ContentDiscovery;

internal sealed class ContentDiscoveryQueryReaderAdapter : IContentDiscoveryQueryReader
{
    private readonly IUnitOfWork _uow;
    private readonly IPrivacyGuard _privacy;

    public ContentDiscoveryQueryReaderAdapter(IUnitOfWork uow, IPrivacyGuard privacy)
    {
        _uow = uow;
        _privacy = privacy;
    }

    // ── Post ─────────────────────────────────────────────────────────────

    public async Task<PostReadModel?> GetPostByIdAsync(Guid postId, Guid? viewerId, CancellationToken ct = default)
    {
        var post = await _uow.Posts.GetPostWithAllAsync(postId);
        if (post is null || post.DeletedDayExpiredAt is not null) return null;
        if (!await _privacy.CanViewPostAsync(post, viewerId)) return null;
        return MapPost(post);
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetProfilePostsAsync(
        Guid profileId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await ProfileExistsAsync(profileId, ct))
            throw new KeyNotFoundException($"Profile '{profileId}' not found.");

        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (!await _privacy.CanViewProfileAsync(profile!, viewerId))
            return ([], 0);

        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetProfilePostsPagedAsync(profileId, skip, pageSize);

        var result = new List<PostReadModel>();
        foreach (var p in posts)
        {
            if (await _privacy.CanViewPostAsync(p, viewerId))
                result.Add(MapPost(p));
        }

        return (result, total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetNewsFeedAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetFeedPagedAsync(userId, skip, pageSize);
        return (posts.Select(MapPost).ToList(), total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetGuestFeedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetGuestFeedPagedAsync(skip, pageSize);
        return (posts.Select(MapPost).ToList(), total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetExploreFeedAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetExploreFeedPagedAsync(userId, skip, pageSize);
        return (posts.Select(MapPost).ToList(), total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetLatestFeedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetLatestPostsPagedAsync(skip, pageSize);
        return (posts.Select(MapPost).ToList(), total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetArchivedPostsAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetArchivedByProfilePagedAsync(userId, skip, pageSize);
        return (posts.Select(MapPost).ToList(), total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetRecycleBinAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetRecycleBinByProfilePagedAsync(userId, skip, pageSize);
        return (posts.Select(MapPost).ToList(), total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> SearchPostsAsync(
        string query, Guid? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var posts = await _uow.Posts.SearchPostsByCaptionAsync(query, skip, pageSize);
        var result = new List<PostReadModel>();
        foreach (var p in posts)
        {
            if (await _privacy.CanViewPostAsync(p, userId))
                result.Add(MapPost(p));
        }
        return (result, result.Count);
    }

    // ── Repost ───────────────────────────────────────────────────────────

    public async Task<RepostReadModel?> GetRepostByIdAsync(Guid repostId, Guid? viewerId, CancellationToken ct = default)
    {
        var repost = await _uow.Reposts.GetRepostByIdAsync(repostId);
        if (repost is null) return null;

        var repostCount = await _uow.Reposts.GetRepostCountAsync(repost.OriginalPostId);
        var hasReposted = viewerId.HasValue && await _uow.Reposts.HasRepostedAsync(viewerId.Value, repost.OriginalPostId);

        return MapRepost(repost, repostCount, hasReposted);
    }

    public async Task<(IReadOnlyList<RepostReadModel> Items, int TotalCount)> GetRepostsByProfileAsync(
        Guid profileId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (reposts, total) = await _uow.Reposts.GetRepostsByProfilePagedAsync(profileId, skip, pageSize);

        var result = new List<RepostReadModel>();
        foreach (var r in reposts)
        {
            var repostCount = await _uow.Reposts.GetRepostCountAsync(r.OriginalPostId);
            var hasReposted = viewerId.HasValue && await _uow.Reposts.HasRepostedAsync(viewerId.Value, r.OriginalPostId);
            result.Add(MapRepost(r, repostCount, hasReposted));
        }

        return (result, total);
    }

    // ── Feed with reposts ────────────────────────────────────────────────

    public async Task<(IReadOnlyList<FeedItemReadModel> Items, int TotalCount)> GetFeedWithRepostsAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var (posts, _) = await _uow.Posts.GetFeedPagedAsync(userId, 0, 200);
        var reposts = await _uow.Reposts.GetFeedRepostsAsync(userId, 0, 200);

        var feedItems = new List<FeedItemReadModel>();

        foreach (var p in posts)
            feedItems.Add(new FeedItemReadModel(FeedItemKind.Post, MapPost(p), null, p.CreatedAt));

        foreach (var r in reposts)
        {
            var repostCount = await _uow.Reposts.GetRepostCountAsync(r.OriginalPostId);
            var hasReposted = await _uow.Reposts.HasRepostedAsync(userId, r.OriginalPostId);
            feedItems.Add(new FeedItemReadModel(FeedItemKind.Repost, null, MapRepost(r, repostCount, hasReposted), r.CreatedAt));
        }

        var sorted = feedItems
            .OrderByDescending(f => f.CreatedAt)
            .ToList();

        var total = sorted.Count;
        var paged = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (paged, total);
    }

    // ── Collection ───────────────────────────────────────────────────────

    public async Task<CollectionReadModel?> GetCollectionByIdAsync(
        Guid collectionId, Guid? viewerId, CancellationToken ct = default)
    {
        var collection = await _uow.Collections.GetByIdAsync(collectionId);
        if (collection is null) return null;
        if (!await _privacy.CanViewCollectionAsync(collection, viewerId)) return null;

        var withPosts = await _uow.Collections.GetCollectionWithPostsAsync(collectionId);
        return MapCollection(withPosts);
    }

    public async Task<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)> GetCollectionsByOwnerAsync(
        Guid ownerId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (collections, total) = await _uow.Collections.GetAllByOwnerPagedAsync(ownerId, skip, pageSize);

        var result = new List<CollectionReadModel>();
        foreach (var c in collections)
        {
            if (await _privacy.CanViewCollectionAsync(c, viewerId))
                result.Add(MapCollection(c));
        }

        return (result, total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetCollectionPostsAsync(
        Guid collectionId, Guid? viewerId, int page, int pageSize, CancellationToken ct = default)
    {
        var collection = await _uow.Collections.GetByIdAsync(collectionId);
        if (collection is null || !await _privacy.CanViewCollectionAsync(collection, viewerId))
            return ([], 0);

        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetPostsByCollectionPagedAsync(collectionId, skip, pageSize);
        return (posts.Select(MapPost).ToList(), total);
    }

    public async Task<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)> GetTrendingCollectionsAsync(
        Guid? viewerId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (collections, total) = await _uow.Collections.GetAllPagedAsync(skip, pageSize);

        var result = new List<CollectionReadModel>();
        foreach (var c in collections)
        {
            if (await _privacy.CanViewCollectionAsync(c, viewerId))
                result.Add(MapCollection(c));
        }

        return (result, total);
    }

    // ── Mappers ──────────────────────────────────────────────────────────

    private static PostReadModel MapPost(Post post) => new(
        post.Id,
        post.ProfileId,
        post.Caption,
        post.CreatedAt,
        post.UpdatedAt,
        (int)post.Privacy,
        post.PostMedias
            .OrderBy(m => m.Position)
            .Select(m => new PostMediaReadModel(
                m.Id, m.PostId ?? Guid.Empty, m.Url, m.PublicId,
                m.Width, m.Height, m.Format, m.Position, m.ThumbnailUrl))
            .ToList(),
        post.PostTags
            .Where(pt => pt.Tag is not null)
            .Select(pt => new TagReadModel(pt.Tag.Id, pt.Tag.Name))
            .ToList(),
        post.LocationName is not null
            ? new PostLocationReadModel(
                post.LocationName, post.LocationFullAddress,
                post.LocationLatitude, post.LocationLongitude)
            : null,
        post.IsNSFW,
        post.Comments.Count);

    private static RepostReadModel MapRepost(Repost repost, int repostCount, bool isRepostedByCurrentUser) => new(
        repost.Id,
        repost.ProfileId,
        repost.Profile.Username,
        repost.Profile.DisplayName,
        repost.Profile.AvatarUrl,
        repost.OriginalPostId,
        repost.OriginalPost.Caption,
        repost.OriginalPost.ProfileId,
        repost.OriginalPost.Profile.Username,
        repost.OriginalPost.Profile.DisplayName,
        repost.OriginalPost.Profile.AvatarUrl,
        repost.OriginalPost.PostMedias
            .OrderBy(m => m.Position)
            .Select(m => new PostMediaReadModel(
                m.Id, m.PostId ?? Guid.Empty, m.Url, m.PublicId,
                m.Width, m.Height, m.Format, m.Position, m.ThumbnailUrl))
            .ToList(),
        repost.Caption,
        repost.CreatedAt,
        repost.UpdatedAt,
        repost.Comments.Count,
        repostCount,
        isRepostedByCurrentUser);

    private static CollectionReadModel MapCollection(Collection c) => new(
        c.Id,
        c.ProfileId,
        c.Title,
        c.Description,
        c.CoverImageUrl,
        (int)c.PrivacyLevel,
        c.CreatedAt,
        c.UpdatedAt,
        c.PostCollections.Select(pc => pc.PostId).ToList(),
        c.PostCollections.Count);
}

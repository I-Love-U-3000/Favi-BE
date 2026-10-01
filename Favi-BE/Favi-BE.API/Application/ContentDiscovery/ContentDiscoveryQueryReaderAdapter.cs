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
        var (posts, total) = await _uow.Posts.GetProfilePostsPagedAsync(profileId, skip, pageSize, viewerId);

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
        var now = DateTime.UtcNow;

        // 1. Gather candidate posts (70% network, 30% discovery)
        var networkCandidates = await _uow.Posts.GetFeedCandidatesAsync(userId, 200, ct);
        var discoveryCandidates = await _uow.Posts.GetDiscoveryCandidatesAsync(userId, 100, ct);

        // Deduplicate candidates
        var allCandidatesDict = new Dictionary<Guid, Post>();
        foreach (var p in networkCandidates) allCandidatesDict[p.Id] = p;
        foreach (var p in discoveryCandidates) allCandidatesDict.TryAdd(p.Id, p);
        var candidates = allCandidatesDict.Values.ToList();

        if (candidates.Count == 0)
        {
            return ([], 0);
        }

        // 2. Extract viewer social signals
        var followedIds = (await _uow.Follows.GetFolloweeIdsAsync(userId, ct)).ToHashSet();
        var followerIds = (await _uow.Follows.GetFollowerIdsAsync(userId, ct)).ToHashSet();

        var recentReactions = await _uow.Reactions.GetRecentReactionsByProfileIdAsync(userId, now.AddDays(-30), ct);
        var authorInteractionCounts = recentReactions
            .Where(r => r.Post != null)
            .GroupBy(r => r.Post!.ProfileId)
            .ToDictionary(g => g.Key, g => g.Count());

        // 3. Score candidate posts
        var scoredPosts = new List<(Post Post, double Score)>();

        foreach (var post in candidates)
        {
            if (!await _privacy.CanViewPostAsync(post, userId))
                continue;

            var authorId = post.ProfileId;
            var isSelf = authorId == userId;
            var isFollowed = followedIds.Contains(authorId);
            var isMutual = followerIds.Contains(authorId);

            // A. Social Affinity Score A(u, a) in [1.0, 5.0]
            double affinity = 1.0;
            if (isSelf)
            {
                affinity += 1.0;
            }
            else if (isFollowed)
            {
                affinity += 1.5;
                if (isMutual) affinity += 0.5;
            }

            if (authorInteractionCounts.TryGetValue(authorId, out var count))
            {
                affinity += Math.Min(2.0, count * 0.4);
            }

            // B. Quality & Engagement Score Q(p)
            var reactions = post.Reactions ?? (ICollection<Reaction>)Array.Empty<Reaction>();
            double weightedReactions = 0;
            foreach (var r in reactions)
            {
                weightedReactions += r.Type switch
                {
                    Favi_BE.Models.Enums.ReactionType.Love => 1.5,
                    Favi_BE.Models.Enums.ReactionType.Wow => 1.2,
                    Favi_BE.Models.Enums.ReactionType.Haha => 1.0,
                    Favi_BE.Models.Enums.ReactionType.Like => 1.0,
                    Favi_BE.Models.Enums.ReactionType.Sad => 0.8,
                    Favi_BE.Models.Enums.ReactionType.Angry => 0.5,
                    _ => 1.0
                };
            }
            var commentCount = post.Comments?.Count ?? 0;
            var quality = Math.Log10(1.0 + weightedReactions + (2.0 * commentCount));

            // C. Topic / Tag Overlap T(u, p)
            double topicScore = 0.0;
            if (post.PostTags != null && post.PostTags.Any())
            {
                topicScore = Math.Min(1.5, post.PostTags.Count * 0.3);
            }

            // D. Time Decay (Half-life ~ 14 hours => lambda = 0.05)
            var ageHours = Math.Max(0, (now - post.CreatedAt).TotalHours);
            var decay = Math.Exp(-0.05 * ageHours);

            var baseScore = ((2.5 * affinity) + (1.5 * quality) + (1.0 * topicScore)) * decay;
            scoredPosts.Add((post, baseScore));
        }

        // 4. Sort and apply Diversity & Anti-Fatigue Damping (damping multiple posts by same author)
        var authorPostCount = new Dictionary<Guid, int>();
        var finalRanked = scoredPosts
            .OrderByDescending(x => x.Score)
            .Select(x =>
            {
                var authorId = x.Post.ProfileId;
                authorPostCount.TryGetValue(authorId, out var seenCount);
                authorPostCount[authorId] = seenCount + 1;

                var multiplier = seenCount switch
                {
                    0 => 1.0,
                    1 => 0.65,
                    _ => 0.40
                };

                return (x.Post, AdjustedScore: x.Score * multiplier);
            })
            .OrderByDescending(x => x.AdjustedScore)
            .Select(x => x.Post)
            .ToList();

        var total = finalRanked.Count;
        var skip = (page - 1) * pageSize;
        var pagePosts = finalRanked.Skip(skip).Take(pageSize).Select(MapPost).ToList();

        return (pagePosts, total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetGuestFeedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var twoHoursAgo = now.AddHours(-2);

        var candidates = await _uow.Posts.GetGuestFeedCandidatesAsync(200, ct);

        var scored = new List<(Post Post, double Score)>();
        foreach (var post in candidates)
        {
            if (!await _privacy.CanViewPostAsync(post, null))
                continue;

            var reactions = post.Reactions ?? (ICollection<Reaction>)Array.Empty<Reaction>();
            var reactionCount = reactions.Count;
            var recentReactions = reactions.Count(r => r.CreatedAt >= twoHoursAgo);
            var commentCount = post.Comments?.Count ?? 0;

            // Velocity in last 2 hours
            var velocity = recentReactions / 2.0;

            var ageHours = Math.Max(0, (now - post.CreatedAt).TotalHours);
            var decay = Math.Exp(-0.08 * ageHours);

            var score = (1.0 + (1.0 * reactionCount) + (2.5 * commentCount))
                        * decay
                        * (1.0 + (0.5 * velocity));

            scored.Add((post, score));
        }

        // Apply author damping for guest feed
        var authorPostCount = new Dictionary<Guid, int>();
        var finalRanked = scored
            .OrderByDescending(x => x.Score)
            .Select(x =>
            {
                var authorId = x.Post.ProfileId;
                authorPostCount.TryGetValue(authorId, out var seenCount);
                authorPostCount[authorId] = seenCount + 1;

                var multiplier = seenCount switch
                {
                    0 => 1.0,
                    1 => 0.65,
                    _ => 0.40
                };

                return (x.Post, AdjustedScore: x.Score * multiplier);
            })
            .OrderByDescending(x => x.AdjustedScore)
            .Select(x => x.Post)
            .ToList();

        var total = finalRanked.Count;
        var skip = (page - 1) * pageSize;
        var pagePosts = finalRanked.Skip(skip).Take(pageSize).Select(MapPost).ToList();

        return (pagePosts, total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetExploreFeedAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetExploreFeedPagedAsync(userId, skip, pageSize);
        var result = new List<PostReadModel>();
        foreach (var p in posts)
        {
            if (await _privacy.CanViewPostAsync(p, userId))
                result.Add(MapPost(p));
        }
        return (result, total);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetLatestFeedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (posts, total) = await _uow.Posts.GetLatestPostsPagedAsync(skip, pageSize);
        var result = new List<PostReadModel>();
        foreach (var p in posts)
        {
            if (await _privacy.CanViewPostAsync(p, null))
                result.Add(MapPost(p));
        }
        return (result, total);
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

        if (repost.OriginalPost == null || repost.OriginalPost.DeletedDayExpiredAt != null || repost.OriginalPost.IsArchived)
            return null;

        if (!await _privacy.CanViewPostAsync(repost.OriginalPost, viewerId))
            return null;

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
            if (r.OriginalPost == null || r.OriginalPost.DeletedDayExpiredAt != null || r.OriginalPost.IsArchived)
                continue;

            if (!await _privacy.CanViewPostAsync(r.OriginalPost, viewerId))
                continue;

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
        {
            if (await _privacy.CanViewPostAsync(p, userId))
                feedItems.Add(new FeedItemReadModel(FeedItemKind.Post, MapPost(p), null, p.CreatedAt));
        }

        foreach (var r in reposts)
        {
            if (r.OriginalPost == null || r.OriginalPost.DeletedDayExpiredAt != null || r.OriginalPost.IsArchived)
                continue;

            if (!await _privacy.CanViewPostAsync(r.OriginalPost, userId))
                continue;

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
        var result = new List<PostReadModel>();
        foreach (var p in posts)
        {
            if (await _privacy.CanViewPostAsync(p, viewerId))
                result.Add(MapPost(p));
        }
        return (result, total);
    }

    public async Task<(IReadOnlyList<CollectionReadModel> Items, int TotalCount)> GetTrendingCollectionsAsync(
        Guid? viewerId, int page, int pageSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var recentWindow = now.AddHours(-48);
        const double lambda = 0.03; // Smooth decay over ~3 days

        // 1. Fetch trending candidates (up to 100)
        var candidates = await _uow.Collections.GetTrendingCandidatesAsync(100, ct);

        // 2. Score candidates
        var scored = new List<(Collection Collection, double Score)>();
        foreach (var c in candidates)
        {
            if (!await _privacy.CanViewCollectionAsync(c, viewerId))
                continue;

            var postCount = c.PostCollections?.Count ?? 0;
            if (postCount == 0) continue;

            var totalReactions = c.Reactions?.Count ?? 0;
            var recentReactions = c.Reactions?.Count(r => r.CreatedAt >= recentWindow) ?? 0;

            // Effective last active time (creation or last update)
            var lastActive = c.UpdatedAt > c.CreatedAt ? c.UpdatedAt : c.CreatedAt;
            var ageHours = Math.Max(0, (now - lastActive).TotalHours);

            // Formula: log2(1 + N_posts) * (1.0 + R_all + 3.0 * R_recent_48h) * e^(-lambda * ageHours)
            var score = Math.Log2(1.0 + postCount)
                        * (1.0 + totalReactions + (3.0 * recentReactions))
                        * Math.Exp(-lambda * ageHours);

            scored.Add((c, score));
        }

        // 3. Sort by score descending and paginate
        var ordered = scored.OrderByDescending(x => x.Score).Select(x => x.Collection).ToList();
        var total = ordered.Count;
        var skip = (page - 1) * pageSize;
        var paged = ordered.Skip(skip).Take(pageSize).Select(MapCollection).ToList();

        return (paged, total);
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

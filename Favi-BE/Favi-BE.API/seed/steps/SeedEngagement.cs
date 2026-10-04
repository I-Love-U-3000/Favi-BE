using System.Globalization;
using System.Text;
using System.Text.Json;
using Favi_BE.API.Models.Entities;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedEngagementStep
{
    private const double ReplyRate = 0.30;
    private const double CommentUrlRate = 0.10;

    private static readonly string[] CommentTemplates =
    [
        "Hay quá!",
        "Chuẩn luôn, đồng ý 100%.",
        "Bài này hữu ích thật.",
        "Có ai thử cách này chưa?",
        "Nhìn cuốn quá.",
        "Cảm ơn đã chia sẻ!",
        "Quan điểm này khá thuyết phục.",
        "Ý này đáng để follow-up."
    ];

    private static readonly string[] CommentLinkDomains =
    [
        "example.com",
        "docs.example.org",
        "blog.seed.local",
        "news.example.net"
    ];

    private enum PostTier
    {
        Viral = 0,    // 105 posts, 1005-1030 reactions (meets SeedValidator >= 100 posts with 1000+ rx)
        Trending = 1, // 300 posts, 60-200 reactions
        Active = 2,   // 1000 posts, 15-60 reactions
        Moderate = 3, // 2000 posts, 4-15 reactions
        Cold = 4      // ~1595 posts, 1-4 reactions
    }

    private sealed class PostEngagementMeta
    {
        public required Post Post { get; init; }
        public required PostTier Tier { get; init; }
        public required int TargetReactions { get; init; }
    }

    public async Task<SeedEngagementResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Post> posts,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (profiles.Count == 0 || posts.Count == 0)
            throw new InvalidOperationException("Step 4 requires profiles and posts from earlier steps.");

        // 1. Pre-load followers for authors of friend-only posts to respect privacy rules
        var followersPostsAuthors = posts
            .Where(p => p.Privacy == PrivacyLevel.Followers)
            .Select(p => p.ProfileId)
            .Distinct()
            .ToList();

        var followersByAuthor = await db.Follows
            .AsNoTracking()
            .Where(f => followersPostsAuthors.Contains(f.FolloweeId))
            .GroupBy(f => f.FolloweeId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(f => f.FollowerId).ToList(), cancellationToken);

        // 2. Classify posts into realistic engagement tiers (Viral, Trending, Active, Moderate, Cold)
        var postMetas = ClassifyPostTiers(posts, profiles, seedContext);
        var postTierMap = postMetas.ToDictionary(m => m.Post.Id, m => m.Tier);

        // 3. Generate realistic comments correlated with post popularity
        var comments = GenerateComments(profiles, postMetas, followersByAuthor, seedContext);

        // 4. Generate reposts correlated with post popularity
        var reposts = GenerateReposts(profiles, postMetas, seedContext);

        // 5. Generate reactions with realistic heavy-tail post curve, comment reactions, and repost reactions
        var reactions = GenerateAllReactions(profiles, postMetas, comments, reposts, followersByAuthor, postTierMap, seedContext);

        // 6. Comprehensive validation ensuring integrity, causality, and compliance with SeedValidator
        ValidateEngagement(reactions, comments, reposts, profiles, posts, seedContext);

        // 7. Persist in memory-friendly batches in strict topological foreign-key order:
        // (a) Root comments first
        // (b) Reply comments next (referencing root comments)
        // (c) Reposts
        // (d) Reactions last (which reference posts, comments, and reposts)
        var rootComments = comments.Where(c => c.ParentCommentId == null).ToList();
        var replyComments = comments.Where(c => c.ParentCommentId != null).ToList();

        await SaveCommentsInBatchesAsync(db, rootComments, cancellationToken);
        await SaveCommentsInBatchesAsync(db, replyComments, cancellationToken);
        await SaveRepostsInBatchesAsync(db, reposts, cancellationToken);
        await SaveReactionsInBatchesAsync(db, reactions, cancellationToken);

        var reactionsPath = ExportReactionsCsv(reactions);
        var commentsPath = ExportCommentsCsv(comments);
        var repostsPath = ExportRepostsCsv(reposts);

        return new SeedEngagementResult(reactions.Count, comments.Count, reposts.Count, reactionsPath, commentsPath, repostsPath);
    }

    private static List<PostEngagementMeta> ClassifyPostTiers(
        IReadOnlyList<Post> posts,
        IReadOnlyList<Profile> profiles,
        SeedContext seedContext)
    {
        var postById = posts.ToDictionary(p => p.Id);
        var assignedPostIds = new HashSet<Guid>();
        var result = new List<PostEngagementMeta>(posts.Count);

        // Tier 0: Exactly 105 Viral posts. Must be PrivacyLevel.Public.
        // Explicitly map post_0 to post_104 (the first 105 posts in posts.csv and k6 benchmark scripts)
        for (var i = 0; i < 105; i++)
        {
            var viralPostId = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", i);
            if (postById.TryGetValue(viralPostId, out var post) && assignedPostIds.Add(post.Id))
            {
                result.Add(new PostEngagementMeta
                {
                    Post = post,
                    Tier = PostTier.Viral,
                    TargetReactions = seedContext.Random.Next(1005, 1031) // Guarantees >= 1000 for SeedValidator
                });
            }
        }

        // Fallback: If any of post_0..post_104 wasn't found, pick remaining public posts to ensure exactly 105 viral posts
        if (result.Count < 105)
        {
            var candidates = posts
                .Where(p => p.Privacy == PrivacyLevel.Public && !assignedPostIds.Contains(p.Id))
                .ToList();
            foreach (var post in candidates.Take(105 - result.Count))
            {
                assignedPostIds.Add(post.Id);
                result.Add(new PostEngagementMeta
                {
                    Post = post,
                    Tier = PostTier.Viral,
                    TargetReactions = seedContext.Random.Next(1005, 1031)
                });
            }
        }

        var remainingPosts = posts
            .Where(p => !assignedPostIds.Contains(p.Id))
            .OrderBy(p => StableSeed.FromString($"{seedContext.SeedKey}:post-tier:{p.Id}"))
            .ToList();

        var index = 0;

        // 300 Trending Posts (60-200 likes)
        var trendingLimit = Math.Min(300, remainingPosts.Count);
        for (; index < trendingLimit; index++)
        {
            result.Add(new PostEngagementMeta
            {
                Post = remainingPosts[index],
                Tier = PostTier.Trending,
                TargetReactions = seedContext.Random.Next(60, 201)
            });
        }

        // 1000 Active Posts (15-60 likes)
        var activeLimit = Math.Min(index + 1000, remainingPosts.Count);
        for (; index < activeLimit; index++)
        {
            result.Add(new PostEngagementMeta
            {
                Post = remainingPosts[index],
                Tier = PostTier.Active,
                TargetReactions = seedContext.Random.Next(15, 61)
            });
        }

        // 2000 Moderate Posts (4-15 likes)
        var moderateLimit = Math.Min(index + 2000, remainingPosts.Count);
        for (; index < moderateLimit; index++)
        {
            result.Add(new PostEngagementMeta
            {
                Post = remainingPosts[index],
                Tier = PostTier.Moderate,
                TargetReactions = seedContext.Random.Next(4, 16)
            });
        }

        // Remaining Cold Posts (1-4 likes)
        for (; index < remainingPosts.Count; index++)
        {
            result.Add(new PostEngagementMeta
            {
                Post = remainingPosts[index],
                Tier = PostTier.Cold,
                TargetReactions = seedContext.Random.Next(1, 5)
            });
        }

        return result;
    }

    private static List<Reaction> GenerateAllReactions(
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<PostEngagementMeta> postMetas,
        IReadOnlyList<Comment> comments,
        IReadOnlyList<Repost> reposts,
        IReadOnlyDictionary<Guid, List<Guid>> followersByAuthor,
        IReadOnlyDictionary<Guid, PostTier> postTierMap,
        SeedContext seedContext)
    {
        var targetTotal = seedContext.Random.Next(SeedConfig.Reactions.Min, SeedConfig.Reactions.Max + 1);
        var reactions = new List<Reaction>(targetTotal);

        var postPairSet = new HashSet<(Guid PostId, Guid ProfileId)>();
        var commentPairSet = new HashSet<(Guid CommentId, Guid ProfileId)>();
        var repostPairSet = new HashSet<(Guid RepostId, Guid ProfileId)>();

        // Pre-group profiles by activity role
        var powerProfiles = profiles.Where(p => InferActivityRole(p) == "power").ToList();
        var casualProfiles = profiles.Where(p => InferActivityRole(p) == "casual").ToList();
        var lurkerProfiles = profiles.Where(p => InferActivityRole(p) == "lurker").ToList();

        // 1. Generate Post Reactions adhering to natural curve & privacy
        foreach (var meta in postMetas)
        {
            var post = meta.Post;
            var targetCount = meta.TargetReactions;

            if (post.Privacy == PrivacyLevel.Followers)
            {
                if (followersByAuthor.TryGetValue(post.ProfileId, out var followers) && followers.Count > 0)
                {
                    var eligibleFollowers = followers.Where(fid => fid != post.ProfileId).ToList();
                    var countToTake = Math.Min(targetCount, eligibleFollowers.Count);
                    ShuffleList(eligibleFollowers, seedContext);

                    foreach (var reactorId in eligibleFollowers.Take(countToTake))
                    {
                        if (postPairSet.Add((post.Id, reactorId)))
                        {
                            reactions.Add(new Reaction
                            {
                                Id = Guid.NewGuid(),
                                PostId = post.Id,
                                ProfileId = reactorId,
                                Type = PickReactionType(seedContext),
                                CreatedAt = BuildCausalTimestamp(post.CreatedAt, seedContext)
                            });
                        }
                    }
                }
                continue;
            }

            // Public posts
            if (meta.Tier == PostTier.Viral)
            {
                var eligibleProfiles = profiles.Where(p => p.Id != post.ProfileId).ToList();
                ShuffleList(eligibleProfiles, seedContext);

                // Prioritize power and casual users, then lurkers, with deterministic tie-breaking
                var prioritizedReactors = eligibleProfiles
                    .OrderByDescending(p => InferActivityRole(p) == "power" ? 3 : (InferActivityRole(p) == "casual" ? 2 : 1))
                    .ThenBy(p => StableSeed.FromString($"{seedContext.SeedKey}:{post.Id}:rx:{p.Id}"))
                    .Take(targetCount)
                    .ToList();

                foreach (var reactor in prioritizedReactors)
                {
                    if (postPairSet.Add((post.Id, reactor.Id)))
                    {
                        reactions.Add(new Reaction
                        {
                            Id = Guid.NewGuid(),
                            PostId = post.Id,
                            ProfileId = reactor.Id,
                            Type = PickReactionType(seedContext),
                            CreatedAt = BuildCausalTimestamp(post.CreatedAt, seedContext)
                        });
                    }
                }
            }
            else
            {
                // Trending, Active, Moderate, Cold
                var attempts = 0;
                var maxAttempts = targetCount * 10;
                while (postPairSet.Count(p => p.PostId == post.Id) < targetCount && attempts < maxAttempts)
                {
                    attempts++;
                    var profile = PickProfileWeightedByActivity(profiles, seedContext);
                    if (profile.Id == post.ProfileId)
                        continue;

                    if (postPairSet.Add((post.Id, profile.Id)))
                    {
                        reactions.Add(new Reaction
                        {
                            Id = Guid.NewGuid(),
                            PostId = post.Id,
                            ProfileId = profile.Id,
                            Type = PickReactionType(seedContext),
                            CreatedAt = BuildCausalTimestamp(post.CreatedAt, seedContext)
                        });
                    }
                }
            }
        }

        // 2. High-performance seed requirements: root comments on top viral posts have 1000+ reactions (>= 2 cases)
        var post0Id = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", 0);
        var post1Id = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", 1);
        var post2Id = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", 2);

        var megaComment0 = comments.FirstOrDefault(c => c.PostId == post0Id && c.ParentCommentId == null);
        var megaComment1 = comments.FirstOrDefault(c => c.PostId == post1Id && c.ParentCommentId == null);
        var megaComment2 = comments.FirstOrDefault(c => c.PostId == post2Id && c.ParentCommentId == null);

        void AddMegaCommentReactions(Comment comment, int targetRx)
        {
            var eligible = profiles
                .Where(p => p.Id != comment.ProfileId)
                .OrderBy(p => StableSeed.FromString($"{seedContext.SeedKey}:{comment.Id}:crx:{p.Id}"))
                .Take(targetRx)
                .ToList();

            foreach (var p in eligible)
            {
                if (commentPairSet.Add((comment.Id, p.Id)))
                {
                    reactions.Add(new Reaction
                    {
                        Id = Guid.NewGuid(),
                        CommentId = comment.Id,
                        ProfileId = p.Id,
                        Type = PickReactionType(seedContext),
                        CreatedAt = BuildCausalTimestamp(comment.CreatedAt, seedContext)
                    });
                }
            }
        }

        if (megaComment0 != null) AddMegaCommentReactions(megaComment0, 1025);
        if (megaComment1 != null) AddMegaCommentReactions(megaComment1, 1015);
        if (megaComment2 != null) AddMegaCommentReactions(megaComment2, 1005);

        // 3. Generate Comment and Repost reactions to reach total reaction quota
        var remainingNeeded = Math.Max(0, targetTotal - reactions.Count);
        if (remainingNeeded > 0 && (comments.Count > 0 || reposts.Count > 0))
        {
            var commentQuota = reposts.Count > 0 ? (int)Math.Round(remainingNeeded * 0.72) : remainingNeeded;
            var repostQuota = remainingNeeded - commentQuota;

            // Comment reactions
            var commentAttempts = 0;
            var maxCommentAttempts = commentQuota * 15;
            while (commentQuota > 0 && comments.Count > 0 && commentAttempts < maxCommentAttempts)
            {
                commentAttempts++;
                var comment = PickCommentWeightedByTier(comments, postTierMap, seedContext);
                var profile = PickProfileWeightedByActivity(profiles, seedContext);
                if (profile.Id == comment.ProfileId)
                    continue;

                if (commentPairSet.Add((comment.Id, profile.Id)))
                {
                    reactions.Add(new Reaction
                    {
                        Id = Guid.NewGuid(),
                        CommentId = comment.Id,
                        ProfileId = profile.Id,
                        Type = PickReactionType(seedContext),
                        CreatedAt = BuildCausalTimestamp(comment.CreatedAt, seedContext)
                    });
                    commentQuota--;
                }
            }

            // Repost reactions
            var repostAttempts = 0;
            var maxRepostAttempts = repostQuota * 15;
            while (repostQuota > 0 && reposts.Count > 0 && repostAttempts < maxRepostAttempts)
            {
                repostAttempts++;
                var repost = reposts[seedContext.Random.Next(reposts.Count)];
                var profile = PickProfileWeightedByActivity(profiles, seedContext);
                if (profile.Id == repost.ProfileId)
                    continue;

                if (repostPairSet.Add((repost.Id, profile.Id)))
                {
                    reactions.Add(new Reaction
                    {
                        Id = Guid.NewGuid(),
                        RepostId = repost.Id,
                        ProfileId = profile.Id,
                        Type = PickReactionType(seedContext),
                        CreatedAt = BuildCausalTimestamp(repost.CreatedAt, seedContext)
                    });
                    repostQuota--;
                }
            }

            // Top-up with comment reactions if any remaining quota left
            while (reactions.Count < targetTotal && comments.Count > 0 && commentAttempts < maxCommentAttempts * 2)
            {
                commentAttempts++;
                var comment = PickCommentWeightedByTier(comments, postTierMap, seedContext);
                var profile = PickProfileWeightedByActivity(profiles, seedContext);
                if (profile.Id == comment.ProfileId)
                    continue;

                if (commentPairSet.Add((comment.Id, profile.Id)))
                {
                    reactions.Add(new Reaction
                    {
                        Id = Guid.NewGuid(),
                        CommentId = comment.Id,
                        ProfileId = profile.Id,
                        Type = PickReactionType(seedContext),
                        CreatedAt = BuildCausalTimestamp(comment.CreatedAt, seedContext)
                    });
                }
            }
        }

        return reactions;
    }

    private static List<Comment> GenerateComments(
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<PostEngagementMeta> postMetas,
        IReadOnlyDictionary<Guid, List<Guid>> followersByAuthor,
        SeedContext seedContext)
    {
        // 1. Identify top viral posts that must have 1000+ comments
        // Specifically post_0, post_1, post_2, post_3 (the benchmark targets)
        var topViralMetas = postMetas
            .Where(m => m.Tier == PostTier.Viral)
            .Take(4)
            .ToList();

        var topViralTargets = new Dictionary<Guid, int>();
        if (topViralMetas.Count >= 4)
        {
            topViralTargets[topViralMetas[0].Post.Id] = 1050; // post_0 (scenario-b3, scenario-5)
            topViralTargets[topViralMetas[1].Post.Id] = 1030; // post_1 (scenario-a4)
            topViralTargets[topViralMetas[2].Post.Id] = 1015; // post_2
            topViralTargets[topViralMetas[3].Post.Id] = 1005; // post_3
        }

        var topViralCommentQuota = topViralTargets.Values.Sum(); // 4100 comments

        // Total comments quota within SeedConfig.Comments [5000, 15000]
        var targetTotal = seedContext.Random.Next(12000, 13001);
        var remainingQuota = Math.Max(0, targetTotal - topViralCommentQuota);

        var catalog = TryLoadRealCommentsCatalog();
        var hasCatalog = catalog != null && catalog.Count > 0;

        var results = new List<Comment>(targetTotal);
        var rootCommentsByPost = postMetas.ToDictionary(m => m.Post.Id, _ => new List<Comment>());

        var mediaItems = hasCatalog
            ? catalog!.Where(c => c.HasMedia && (!string.IsNullOrWhiteSpace(c.Url) || !string.IsNullOrWhiteSpace(c.LocalPath))).ToList()
            : [];
        var mediaAssignments = 0;
        var commentIndex = 0;

        // Helper to generate a single comment for a post
        Comment AddCommentForPost(Post post, Guid? forcedParentId = null)
        {
            // Pick commenter adhering to privacy
            Guid commenterId;
            if (post.Privacy == PrivacyLevel.Followers && followersByAuthor.TryGetValue(post.ProfileId, out var followers) && followers.Count > 0)
            {
                var eligibleFollowers = followers.Where(fid => fid != post.ProfileId).ToList();
                commenterId = eligibleFollowers.Count > 0
                    ? eligibleFollowers[seedContext.Random.Next(eligibleFollowers.Count)]
                    : followers[seedContext.Random.Next(followers.Count)];
            }
            else
            {
                var profile = PickProfileWeightedByActivity(profiles, seedContext);
                commenterId = profile.Id;
            }

            // Decide root comment vs reply (strictly depth <= 2: parent must be a root comment)
            Guid? parentId = null;
            DateTime commentCreatedAt;
            var postRootComments = rootCommentsByPost[post.Id];

            if (forcedParentId.HasValue)
            {
                parentId = forcedParentId.Value;
                var parent = postRootComments.FirstOrDefault(c => c.Id == forcedParentId.Value);
                var parentTime = parent?.CreatedAt ?? post.CreatedAt;
                commentCreatedAt = BuildCausalTimestamp(parentTime, seedContext);
            }
            else if (postRootComments.Count > 0 && seedContext.Random.NextDouble() < ReplyRate)
            {
                var parent = postRootComments[seedContext.Random.Next(postRootComments.Count)];
                parentId = parent.Id;
                commentCreatedAt = BuildCausalTimestamp(parent.CreatedAt, seedContext);
            }
            else
            {
                commentCreatedAt = BuildCausalTimestamp(post.CreatedAt, seedContext);
            }

            var includeUrl = seedContext.Random.NextDouble() < CommentUrlRate;
            string content;
            string? mediaUrl = null;

            if (hasCatalog)
            {
                var item = catalog![commentIndex % catalog.Count];
                var baseContent = !string.IsNullOrWhiteSpace(item.Content) ? item.Content : CommentTemplates[commentIndex % CommentTemplates.Length];
                if (includeUrl)
                {
                    var domain = CommentLinkDomains[commentIndex % CommentLinkDomains.Length];
                    var slug = $"post-{seedContext.Random.Next(1, 5000):D4}";
                    content = $"{baseContent} Xem thêm: https://{domain}/{slug}";
                }
                else
                {
                    content = baseContent;
                }

                if (mediaAssignments < mediaItems.Count * 2 && seedContext.Random.NextDouble() < 0.25)
                {
                    var mediaItem = mediaItems[mediaAssignments / 2];
                    mediaUrl = !string.IsNullOrWhiteSpace(mediaItem.Url) ? mediaItem.Url : mediaItem.LocalPath;
                    mediaAssignments++;
                }
            }
            else
            {
                content = BuildCommentContent(commentIndex, includeUrl, seedContext);
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                ProfileId = commenterId,
                ParentCommentId = parentId,
                Content = content,
                MediaUrl = mediaUrl,
                CreatedAt = commentCreatedAt,
                UpdatedAt = BuildCausalTimestamp(commentCreatedAt, seedContext)
            };

            if (parentId is null)
                postRootComments.Add(comment);

            results.Add(comment);
            commentIndex++;
            return comment;
        }

        // 2. Generate comments for the top viral posts to guarantee 1000+ comments and mega-threads (>= 2 cases)
        var post0Id = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", 0);
        var post1Id = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", 1);

        foreach (var topMeta in topViralMetas)
        {
            if (topViralTargets.TryGetValue(topMeta.Post.Id, out var needed))
            {
                var post = topMeta.Post;
                if (post.Id == post0Id && needed >= 251)
                {
                    // Root comment 0 followed by 250 direct discussion replies
                    var root = AddCommentForPost(post);
                    for (var r = 0; r < 250; r++)
                    {
                        AddCommentForPost(post, root.Id);
                    }
                    for (var k = 251; k < needed; k++)
                    {
                        AddCommentForPost(post);
                    }
                }
                else if (post.Id == post1Id && needed >= 221)
                {
                    // Root comment 0 followed by 220 direct discussion replies
                    var root = AddCommentForPost(post);
                    for (var r = 0; r < 220; r++)
                    {
                        AddCommentForPost(post, root.Id);
                    }
                    for (var k = 221; k < needed; k++)
                    {
                        AddCommentForPost(post);
                    }
                }
                else
                {
                    for (var k = 0; k < needed; k++)
                    {
                        AddCommentForPost(post);
                    }
                }
            }
        }

        // 3. Distribute remaining comments across other posts by tier weights
        var remainingMetas = postMetas
            .Where(m => !topViralTargets.ContainsKey(m.Post.Id))
            .ToList();

        var postWeights = new double[remainingMetas.Count];
        var totalWeight = 0d;
        for (var i = 0; i < remainingMetas.Count; i++)
        {
            var weight = remainingMetas[i].Tier switch
            {
                PostTier.Viral => 35d,
                PostTier.Trending => 12d,
                PostTier.Active => 4d,
                PostTier.Moderate => 1d,
                _ => 0.20d
            };
            postWeights[i] = weight;
            totalWeight += weight;
        }

        for (var i = 0; i < remainingQuota; i++)
        {
            var roll = seedContext.Random.NextDouble() * totalWeight;
            var chosenMeta = remainingMetas[^1];
            for (var j = 0; j < remainingMetas.Count; j++)
            {
                roll -= postWeights[j];
                if (roll <= 0)
                {
                    chosenMeta = remainingMetas[j];
                    break;
                }
            }

            AddCommentForPost(chosenMeta.Post);
        }

        return results;
    }

    private static List<Repost> GenerateReposts(
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<PostEngagementMeta> postMetas,
        SeedContext seedContext)
    {
        // Only public posts in Viral, Trending, and Active tiers get reposted
        var eligibleMetas = postMetas
            .Where(m => m.Post.Privacy == PrivacyLevel.Public && m.Tier <= PostTier.Active)
            .ToList();

        if (eligibleMetas.Count == 0)
            eligibleMetas = postMetas.Where(m => m.Post.Privacy == PrivacyLevel.Public).ToList();

        var target = seedContext.Random.Next(SeedConfig.Reposts.Min, SeedConfig.Reposts.Max + 1);
        var results = new List<Repost>(target);
        var pairSet = new HashSet<(Guid ProfileId, Guid PostId)>();

        var weights = new double[eligibleMetas.Count];
        var totalWeight = 0d;
        for (var i = 0; i < eligibleMetas.Count; i++)
        {
            var w = eligibleMetas[i].Tier switch
            {
                PostTier.Viral => 10d,
                PostTier.Trending => 4d,
                _ => 1d
            };
            weights[i] = w;
            totalWeight += w;
        }

        var attempts = 0;
        var maxAttempts = target * 20;

        while (results.Count < target && attempts < maxAttempts)
        {
            attempts++;

            var roll = seedContext.Random.NextDouble() * totalWeight;
            var chosenMeta = eligibleMetas[^1];
            for (var j = 0; j < eligibleMetas.Count; j++)
            {
                roll -= weights[j];
                if (roll <= 0)
                {
                    chosenMeta = eligibleMetas[j];
                    break;
                }
            }

            var post = chosenMeta.Post;
            var reposter = PickProfileWeightedByActivity(profiles, seedContext);
            if (reposter.Id == post.ProfileId)
                continue;

            if (!pairSet.Add((reposter.Id, post.Id)))
                continue;

            var createdAt = BuildCausalTimestamp(post.CreatedAt, seedContext);
            results.Add(new Repost
            {
                Id = Guid.NewGuid(),
                ProfileId = reposter.Id,
                OriginalPostId = post.Id,
                Caption = $"Seed repost #{results.Count + 1}",
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        return results;
    }

    private static Comment PickCommentWeightedByTier(
        IReadOnlyList<Comment> comments,
        IReadOnlyDictionary<Guid, PostTier> postTierMap,
        SeedContext seedContext)
    {
        // 5 random candidates tournament
        Comment? best = null;
        var bestScore = -1d;

        for (var i = 0; i < 5; i++)
        {
            var candidate = comments[seedContext.Random.Next(comments.Count)];
            var tierBoost = postTierMap.TryGetValue(candidate.PostId, out var tier)
                ? (tier switch { PostTier.Viral => 8d, PostTier.Trending => 4d, PostTier.Active => 2d, _ => 1d })
                : 1d;
            var depthBoost = candidate.ParentCommentId is null ? 1.5d : 0.8d;
            var score = tierBoost * depthBoost * seedContext.Random.NextDouble();

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best ?? comments[seedContext.Random.Next(comments.Count)];
    }

    private static DateTime BuildCausalTimestamp(DateTime entityCreatedAt, SeedContext seedContext)
    {
        var now = DateTime.UtcNow;
        var diffSeconds = (int)(now - entityCreatedAt).TotalSeconds;
        if (diffSeconds <= 5)
            return now;

        var randomSeconds = seedContext.Random.Next(1, diffSeconds);
        return entityCreatedAt.AddSeconds(randomSeconds);
    }

    private static Profile PickProfileWeightedByActivity(IReadOnlyList<Profile> profiles, SeedContext seedContext)
    {
        var totalWeight = 0d;
        var weights = new double[profiles.Count];

        for (var i = 0; i < profiles.Count; i++)
        {
            var activityRole = InferActivityRole(profiles[i]);
            var weight = activityRole switch
            {
                "power" => 9d,
                "casual" => 3d,
                _ => 0.7d
            };

            weights[i] = weight;
            totalWeight += weight;
        }

        var roll = seedContext.Random.NextDouble() * totalWeight;
        for (var i = 0; i < profiles.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
                return profiles[i];
        }

        return profiles[^1];
    }

    private static void ShuffleList<T>(IList<T> list, SeedContext seedContext)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = seedContext.Random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string InferActivityRole(Profile profile)
    {
        if (profile.Username.StartsWith("user_", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(profile.Username.AsSpan(5), out var indexOneBased))
        {
            var total = SeedConfig.Users.Max;
            var lurkerCutoff = (int)Math.Round(total * SeedConfig.UserRoleDistribution["lurker"], MidpointRounding.AwayFromZero);
            var casualCutoff = lurkerCutoff + (int)Math.Round(total * SeedConfig.UserRoleDistribution["casual"], MidpointRounding.AwayFromZero);

            if (indexOneBased <= lurkerCutoff) return "lurker";
            if (indexOneBased <= casualCutoff) return "casual";
            return "power";
        }

        return "casual";
    }

    private static string BuildCommentContent(int index, bool includeUrl, SeedContext seedContext)
    {
        var baseText = CommentTemplates[index % CommentTemplates.Length];
        if (!includeUrl)
            return $"{baseText} (seed #{index + 1})";

        var domain = CommentLinkDomains[index % CommentLinkDomains.Length];
        var slug = $"post-{seedContext.Random.Next(1, 5000):D4}";
        return $"{baseText} Xem thêm: https://{domain}/{slug}";
    }

    private static ReactionType PickReactionType(SeedContext seedContext)
    {
        var roll = seedContext.Random.NextDouble();
        if (roll < 0.75) return ReactionType.Like;
        if (roll < 0.85) return ReactionType.Love;
        if (roll < 0.92) return ReactionType.Haha;
        if (roll < 0.96) return ReactionType.Wow;
        if (roll < 0.99) return ReactionType.Sad;
        return ReactionType.Angry;
    }

    private static void ValidateEngagement(
        IReadOnlyCollection<Reaction> reactions,
        IReadOnlyCollection<Comment> comments,
        IReadOnlyCollection<Repost> reposts,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Post> posts,
        SeedContext seedContext)
    {
        var postIds = posts.Select(p => p.Id).ToHashSet();
        var profileIds = profiles.Select(p => p.Id).ToHashSet();
        var postById = posts.ToDictionary(p => p.Id);

        if (reactions.GroupBy(r => new { r.PostId, r.ProfileId }).Any(g => g.Key.PostId is not null && g.Count() > 1))
            throw new InvalidOperationException("Validation failed: duplicate reaction pair detected.");

        if (reactions.GroupBy(r => new { r.CommentId, r.ProfileId }).Any(g => g.Key.CommentId is not null && g.Count() > 1))
            throw new InvalidOperationException("Validation failed: duplicate comment reaction pair detected.");

        if (reactions.GroupBy(r => new { r.RepostId, r.ProfileId }).Any(g => g.Key.RepostId is not null && g.Count() > 1))
            throw new InvalidOperationException("Validation failed: duplicate repost reaction pair detected.");

        if (reactions.Any(r =>
                ((r.PostId is not null ? 1 : 0)
                + (r.CommentId is not null ? 1 : 0)
                + (r.RepostId is not null ? 1 : 0)
                + (r.CollectionId is not null ? 1 : 0)) != 1))
            throw new InvalidOperationException("Validation failed: reaction must target exactly one entity.");

        var commentIds = comments.Select(c => c.Id).ToHashSet();
        var commentById = comments.ToDictionary(c => c.Id);
        var repostIds = reposts.Select(r => r.Id).ToHashSet();

        if (reactions.Any(r => !profileIds.Contains(r.ProfileId)))
            throw new InvalidOperationException("Validation failed: reaction has invalid foreign key.");

        if (reactions.Any(r => r.PostId is not null && !postIds.Contains(r.PostId.Value)))
            throw new InvalidOperationException("Validation failed: reaction has invalid PostId foreign key.");

        if (reactions.Any(r => r.CommentId is not null && !commentIds.Contains(r.CommentId.Value)))
            throw new InvalidOperationException("Validation failed: reaction has invalid CommentId foreign key.");

        if (reactions.Any(r => r.RepostId is not null && !repostIds.Contains(r.RepostId.Value)))
            throw new InvalidOperationException("Validation failed: reaction has invalid RepostId foreign key.");

        // Temporal causality validations
        if (reactions.Any(r => r.PostId.HasValue && postById.TryGetValue(r.PostId.Value, out var post) && r.CreatedAt < post.CreatedAt))
            throw new InvalidOperationException("Validation failed: reaction created before target post.");

        if (comments.Any(c => postById.TryGetValue(c.PostId, out var post) && c.CreatedAt < post.CreatedAt))
            throw new InvalidOperationException("Validation failed: comment created before target post.");

        if (comments.Any(c => c.ParentCommentId.HasValue && commentById.TryGetValue(c.ParentCommentId.Value, out var parent) && c.CreatedAt < parent.CreatedAt))
            throw new InvalidOperationException("Validation failed: reply created before parent comment.");

        if (comments.Any(c => c.ParentCommentId is not null && !commentIds.Contains(c.ParentCommentId.Value)))
            throw new InvalidOperationException("Validation failed: orphan comment detected.");

        if (comments.Any(c => c.ParentCommentId is not null
                              && commentById.TryGetValue(c.ParentCommentId.Value, out var parent)
                              && parent.ParentCommentId is not null))
            throw new InvalidOperationException("Validation failed: comment nesting exceeds two levels.");

        if (comments.Any(c => c.ParentCommentId is not null
                              && commentById.TryGetValue(c.ParentCommentId.Value, out var parent)
                              && parent.PostId != c.PostId))
            throw new InvalidOperationException("Validation failed: comment parent and child have different posts.");

        if (!comments.Any(c => c.ParentCommentId is not null))
            throw new InvalidOperationException("Validation failed: no comment replies were generated.");

        if (!comments.Any(c => c.Content.Contains("http://", StringComparison.OrdinalIgnoreCase)
                               || c.Content.Contains("https://", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Validation failed: no comment contains URL.");

        if (!reactions.Any(r => r.CommentId is not null))
            throw new InvalidOperationException("Validation failed: no reaction targets comments.");

        if (comments.Any(c => !postIds.Contains(c.PostId) || !profileIds.Contains(c.ProfileId)))
            throw new InvalidOperationException("Validation failed: comment has invalid foreign key.");

        if (reposts.GroupBy(r => new { r.ProfileId, r.OriginalPostId }).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Validation failed: duplicate repost pair detected.");

        if (reposts.Any(r => !postIds.Contains(r.OriginalPostId) || !profileIds.Contains(r.ProfileId)))
            throw new InvalidOperationException("Validation failed: repost has invalid foreign key.");

        // Validator compatibility check - Reactions
        var postsWith1000 = reactions
            .Where(r => r.PostId.HasValue)
            .GroupBy(r => r.PostId!.Value)
            .Count(g => g.Count() >= 1000);

        if (postsWith1000 < 100)
            throw new InvalidOperationException($"Validation failed: expected at least 100 posts with 1000+ reactions, but got {postsWith1000}.");

        // Validator compatibility check - Comments
        var postsWith1000Comments = comments
            .GroupBy(c => c.PostId)
            .Count(g => g.Count() >= 1000);

        if (postsWith1000Comments < 3)
            throw new InvalidOperationException($"Validation failed: expected at least 3 posts with 1000+ comments, but got {postsWith1000Comments}.");

        var post0Id = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", 0);
        var post0 = posts.FirstOrDefault(p => p.Id == post0Id)
            ?? throw new InvalidOperationException($"Validation failed: post_0 ({post0Id}) not found in posts.");
        var post0Reactions = reactions.Count(r => r.PostId == post0.Id);
        var post0Comments = comments.Count(c => c.PostId == post0.Id);
        if (post0Reactions < 1000 || post0Comments < 1000)
            throw new InvalidOperationException($"Validation failed: post_0 must have >= 1000 reactions and >= 1000 comments, but got {post0Reactions} reactions and {post0Comments} comments.");

        var post1Id = StableSeed.DeterministicGuid(seedContext.SeedKey, "post", 1);
        var post1 = posts.FirstOrDefault(p => p.Id == post1Id)
            ?? throw new InvalidOperationException($"Validation failed: post_1 ({post1Id}) not found in posts.");
        var post1Reactions = reactions.Count(r => r.PostId == post1.Id);
        var post1Comments = comments.Count(c => c.PostId == post1.Id);
        if (post1Reactions < 1000 || post1Comments < 1000)
            throw new InvalidOperationException($"Validation failed: post_1 must have >= 1000 reactions and >= 1000 comments, but got {post1Reactions} reactions and {post1Comments} comments.");

        // Validator check - Comments with 1000+ reactions (>= 2 cases)
        var commentsWith1000Reactions = reactions
            .Where(r => r.CommentId.HasValue)
            .GroupBy(r => r.CommentId!.Value)
            .Count(g => g.Count() >= 1000);

        if (commentsWith1000Reactions < 2)
            throw new InvalidOperationException($"Validation failed: expected at least 2 comments with 1000+ reactions, but got {commentsWith1000Reactions}.");

        // Validator check - Mega discussion threads with 200+ replies (>= 2 cases)
        var megaThreads = comments
            .Where(c => c.ParentCommentId.HasValue)
            .GroupBy(c => c.ParentCommentId!.Value)
            .Count(g => g.Count() >= 200);

        if (megaThreads < 2)
            throw new InvalidOperationException($"Validation failed: expected at least 2 discussion threads with 200+ replies, but got {megaThreads}.");
    }

    private static async Task SaveCommentsInBatchesAsync(
        AppDbContext db,
        List<Comment> comments,
        CancellationToken cancellationToken)
    {
        const int batchSize = 5000;
        for (var i = 0; i < comments.Count; i += batchSize)
        {
            var chunk = comments.Skip(i).Take(batchSize).ToList();
            await db.Comments.AddRangeAsync(chunk, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    private static async Task SaveRepostsInBatchesAsync(
        AppDbContext db,
        List<Repost> reposts,
        CancellationToken cancellationToken)
    {
        const int batchSize = 5000;
        for (var i = 0; i < reposts.Count; i += batchSize)
        {
            var chunk = reposts.Skip(i).Take(batchSize).ToList();
            await db.Reposts.AddRangeAsync(chunk, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    private static async Task SaveReactionsInBatchesAsync(
        AppDbContext db,
        List<Reaction> reactions,
        CancellationToken cancellationToken)
    {
        const int batchSize = 25000;
        for (var i = 0; i < reactions.Count; i += batchSize)
        {
            var chunk = reactions.Skip(i).Take(batchSize).ToList();
            await db.Reactions.AddRangeAsync(chunk, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    private static string ExportReactionsCsv(IEnumerable<Reaction> reactions)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var filePath = Path.Combine(outputRoot, "reactions.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("reaction_id,post_id,profile_id,type,created_at");
        foreach (var reaction in reactions)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{reaction.Id},{reaction.PostId},{reaction.ProfileId},{reaction.Type},{reaction.CreatedAt:O}"));
        }

        return filePath;
    }

    private static string ExportCommentsCsv(IEnumerable<Comment> comments)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var filePath = Path.Combine(outputRoot, "comments.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("comment_id,post_id,profile_id,parent_comment_id,content,media_url,created_at,updated_at");
        foreach (var comment in comments)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{comment.Id},{comment.PostId},{comment.ProfileId},{comment.ParentCommentId},{EscapeCsv(comment.Content)},{EscapeCsv(comment.MediaUrl)},{comment.CreatedAt:O},{comment.UpdatedAt:O}"));
        }

        return filePath;
    }

    private static List<CatalogCommentItem>? TryLoadRealCommentsCatalog()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seed", "catalogs", "real-comments-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "seed", "catalogs", "real-comments-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Favi-BE", "Favi-BE.API", "seed", "catalogs", "real-comments-catalog.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "seed", "catalogs", "real-comments-catalog.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "seed", "catalogs", "real-comments-catalog.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Favi-BE.API", "seed", "catalogs", "real-comments-catalog.json"))
        };

        var catalogPath = candidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(catalogPath))
            return null;

        try
        {
            var json = File.ReadAllText(catalogPath);
            return JsonSerializer.Deserialize<List<CatalogCommentItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static string ExportRepostsCsv(IEnumerable<Repost> reposts)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var filePath = Path.Combine(outputRoot, "reposts.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("repost_id,profile_id,original_post_id,caption,created_at,updated_at");
        foreach (var repost in reposts)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{repost.Id},{repost.ProfileId},{repost.OriginalPostId},{EscapeCsv(repost.Caption)},{repost.CreatedAt:O},{repost.UpdatedAt:O}"));
        }

        return filePath;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}

public readonly record struct SeedEngagementResult(
    int CreatedReactions,
    int CreatedComments,
    int CreatedReposts,
    string ReactionsExportPath,
    string CommentsExportPath,
    string RepostsExportPath);

public sealed class CatalogCommentItem
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("index")]
    public int Index { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("has_media")]
    public bool HasMedia { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("local_path")]
    public string? LocalPath { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}

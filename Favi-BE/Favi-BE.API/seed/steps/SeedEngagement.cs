using System.Globalization;
using System.Text;
using Favi_BE.API.Models.Entities;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedEngagementStep
{
    private const double PostReactionShare = 0.72;
    private const double CommentReactionShare = 0.20;
    private const double RepostReactionShare = 0.08;
    private const double ReplyRate = 0.32;
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

    public async Task<SeedEngagementResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Post> posts,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (profiles.Count == 0 || posts.Count == 0)
            throw new InvalidOperationException("Step 4 requires profiles and posts from earlier steps.");

        var comments = GenerateComments(profiles, posts, seedContext);
        var reposts = GenerateReposts(profiles, posts, seedContext);
        var reactions = GenerateReactions(profiles, posts, comments, reposts, seedContext);

        ValidateEngagement(reactions, comments, reposts, profiles, posts);

        await db.Reactions.AddRangeAsync(reactions, cancellationToken);
        await db.Comments.AddRangeAsync(comments, cancellationToken);
        await db.Reposts.AddRangeAsync(reposts, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var reactionsPath = ExportReactionsCsv(reactions);
        var commentsPath = ExportCommentsCsv(comments);
        var repostsPath = ExportRepostsCsv(reposts);

        return new SeedEngagementResult(reactions.Count, comments.Count, reposts.Count, reactionsPath, commentsPath, repostsPath);
    }

    private static List<Reaction> GenerateReactions(
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Post> posts,
        IReadOnlyList<Comment> comments,
        IReadOnlyList<Repost> reposts,
        SeedContext seedContext)
    {
        var target = seedContext.Random.Next(SeedConfig.Reactions.Min, SeedConfig.Reactions.Max + 1);

        var postQuota = (int)Math.Round(target * PostReactionShare, MidpointRounding.AwayFromZero);
        var commentQuota = (int)Math.Round(target * CommentReactionShare, MidpointRounding.AwayFromZero);
        var repostQuota = Math.Max(0, target - postQuota - commentQuota);

        var postPairSet = new HashSet<(Guid PostId, Guid ProfileId)>();
        var commentPairSet = new HashSet<(Guid CommentId, Guid ProfileId)>();
        var repostPairSet = new HashSet<(Guid RepostId, Guid ProfileId)>();

        var hotPosts = BuildHotPostSet(posts);
        var results = new List<Reaction>(target);
        var attempts = 0;
        var maxAttempts = target * 30;

        while (results.Count < target && attempts < maxAttempts)
        {
            attempts++;

            var generated = false;
            if (postQuota > 0)
            {
                generated = TryCreatePostReaction(results, profiles, posts, hotPosts, seedContext, postPairSet);
                if (generated)
                {
                    postQuota--;
                    continue;
                }
            }

            if (commentQuota > 0)
            {
                generated = TryCreateCommentReaction(results, profiles, comments, hotPosts, seedContext, commentPairSet);
                if (generated)
                {
                    commentQuota--;
                    continue;
                }
            }

            if (repostQuota > 0)
            {
                generated = TryCreateRepostReaction(results, profiles, reposts, hotPosts, seedContext, repostPairSet);
                if (generated)
                {
                    repostQuota--;
                    continue;
                }
            }

            generated = TryCreatePostReaction(results, profiles, posts, hotPosts, seedContext, postPairSet)
                        || TryCreateCommentReaction(results, profiles, comments, hotPosts, seedContext, commentPairSet)
                        || TryCreateRepostReaction(results, profiles, reposts, hotPosts, seedContext, repostPairSet);

            if (!generated)
                break;
        }

        if (results.Count < target)
            throw new InvalidOperationException("Validation failed: unable to generate enough reactions for configured targets.");

        return results;
    }

    private static bool TryCreatePostReaction(
        ICollection<Reaction> results,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Post> posts,
        HashSet<Guid> hotPosts,
        SeedContext seedContext,
        ISet<(Guid PostId, Guid ProfileId)> pairSet)
    {
        if (posts.Count == 0)
            return false;

        var post = PickPostWeighted(posts, hotPosts, seedContext);
        var profile = PickProfileWeightedByActivity(profiles, seedContext);
        if (!pairSet.Add((post.Id, profile.Id)))
            return false;

        results.Add(new Reaction
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            CommentId = null,
            RepostId = null,
            CollectionId = null,
            ProfileId = profile.Id,
            Type = PickReactionType(seedContext),
            CreatedAt = BuildTimestamp(seedContext)
        });

        return true;
    }

    private static bool TryCreateCommentReaction(
        ICollection<Reaction> results,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Comment> comments,
        HashSet<Guid> hotPosts,
        SeedContext seedContext,
        ISet<(Guid CommentId, Guid ProfileId)> pairSet)
    {
        if (comments.Count == 0)
            return false;

        var comment = PickCommentWeighted(comments, hotPosts, seedContext);
        var profile = PickProfileWeightedByActivity(profiles, seedContext);
        if (!pairSet.Add((comment.Id, profile.Id)))
            return false;

        results.Add(new Reaction
        {
            Id = Guid.NewGuid(),
            PostId = null,
            CommentId = comment.Id,
            RepostId = null,
            CollectionId = null,
            ProfileId = profile.Id,
            Type = PickReactionType(seedContext),
            CreatedAt = BuildTimestamp(seedContext)
        });

        return true;
    }

    private static bool TryCreateRepostReaction(
        ICollection<Reaction> results,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Repost> reposts,
        HashSet<Guid> hotPosts,
        SeedContext seedContext,
        ISet<(Guid RepostId, Guid ProfileId)> pairSet)
    {
        if (reposts.Count == 0)
            return false;

        var repost = PickRepostWeighted(reposts, hotPosts, seedContext);
        var profile = PickProfileWeightedByActivity(profiles, seedContext);
        if (!pairSet.Add((repost.Id, profile.Id)))
            return false;

        results.Add(new Reaction
        {
            Id = Guid.NewGuid(),
            PostId = null,
            CommentId = null,
            RepostId = repost.Id,
            CollectionId = null,
            ProfileId = profile.Id,
            Type = PickReactionType(seedContext),
            CreatedAt = BuildTimestamp(seedContext)
        });

        return true;
    }

    private static List<Comment> GenerateComments(IReadOnlyList<Profile> profiles, IReadOnlyList<Post> posts, SeedContext seedContext)
    {
        var target = seedContext.Random.Next(SeedConfig.Comments.Min, SeedConfig.Comments.Max + 1);
        var hotPosts = BuildHotPostSet(posts);
        var results = new List<Comment>(target);
        var rootCommentsByPost = posts.ToDictionary(p => p.Id, _ => new List<Comment>());

        for (var i = 0; i < target; i++)
        {
            var post = PickPostWeighted(posts, hotPosts, seedContext);
            var profile = PickProfileWeightedByActivity(profiles, seedContext);

            Guid? parentId = null;
            if (rootCommentsByPost[post.Id].Count > 0 && seedContext.Random.NextDouble() < ReplyRate)
            {
                var parent = rootCommentsByPost[post.Id][seedContext.Random.Next(rootCommentsByPost[post.Id].Count)];
                parentId = parent.Id;
            }

            var createdAt = BuildTimestamp(seedContext);
            var includeUrl = seedContext.Random.NextDouble() < CommentUrlRate;

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                ProfileId = profile.Id,
                ParentCommentId = parentId,
                Content = BuildCommentContent(i, includeUrl, seedContext),
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddMinutes(seedContext.Random.Next(0, 90))
            };

            if (parentId is null)
                rootCommentsByPost[post.Id].Add(comment);

            results.Add(comment);
        }

        return results;
    }

    private static List<Repost> GenerateReposts(IReadOnlyList<Profile> profiles, IReadOnlyList<Post> posts, SeedContext seedContext)
    {
        var maxPairs = posts.Count * profiles.Count;
        var target = seedContext.Random.Next(SeedConfig.Reposts.Min, SeedConfig.Reposts.Max + 1);
        target = Math.Min(target, maxPairs);

        var results = new List<Repost>(target);
        var pairSet = new HashSet<(Guid ProfileId, Guid PostId)>();
        var hotPosts = BuildHotPostSet(posts);

        while (results.Count < target)
        {
            var post = PickPostWeighted(posts, hotPosts, seedContext);
            var profile = PickProfileWeightedByActivity(profiles, seedContext);
            if (!pairSet.Add((profile.Id, post.Id)))
                continue;

            var createdAt = BuildTimestamp(seedContext);
            results.Add(new Repost
            {
                Id = Guid.NewGuid(),
                ProfileId = profile.Id,
                OriginalPostId = post.Id,
                Caption = $"Seed repost #{results.Count + 1}",
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        return results;
    }

    private static HashSet<Guid> BuildHotPostSet(IReadOnlyList<Post> posts)
    {
        var hotCount = Math.Max(1, (int)Math.Ceiling(posts.Count * 0.08));
        return posts
            .OrderBy(p => StableSeed.FromString($"hot:{p.Id}"))
            .Take(hotCount)
            .Select(p => p.Id)
            .ToHashSet();
    }

    private static Post PickPostWeighted(IReadOnlyList<Post> posts, HashSet<Guid> hotPosts, SeedContext seedContext)
    {
        var totalWeight = 0d;
        var weights = new double[posts.Count];

        for (var i = 0; i < posts.Count; i++)
        {
            var weight = hotPosts.Contains(posts[i].Id) ? 7d : 1d;
            weights[i] = weight;
            totalWeight += weight;
        }

        var roll = seedContext.Random.NextDouble() * totalWeight;
        for (var i = 0; i < posts.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
                return posts[i];
        }

        return posts[^1];
    }

    private static Comment PickCommentWeighted(IReadOnlyList<Comment> comments, HashSet<Guid> hotPosts, SeedContext seedContext)
    {
        var totalWeight = 0d;
        var weights = new double[comments.Count];

        for (var i = 0; i < comments.Count; i++)
        {
            var baseWeight = hotPosts.Contains(comments[i].PostId) ? 6d : 1d;
            var depthBoost = comments[i].ParentCommentId is null ? 1.4d : 0.9d;
            var weight = baseWeight * depthBoost;

            weights[i] = weight;
            totalWeight += weight;
        }

        var roll = seedContext.Random.NextDouble() * totalWeight;
        for (var i = 0; i < comments.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
                return comments[i];
        }

        return comments[^1];
    }

    private static Repost PickRepostWeighted(IReadOnlyList<Repost> reposts, HashSet<Guid> hotPosts, SeedContext seedContext)
    {
        var totalWeight = 0d;
        var weights = new double[reposts.Count];

        for (var i = 0; i < reposts.Count; i++)
        {
            var weight = hotPosts.Contains(reposts[i].OriginalPostId) ? 5d : 1d;
            weights[i] = weight;
            totalWeight += weight;
        }

        var roll = seedContext.Random.NextDouble() * totalWeight;
        for (var i = 0; i < reposts.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
                return reposts[i];
        }

        return reposts[^1];
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

    private static DateTime BuildTimestamp(SeedContext seedContext)
    {
        var now = DateTime.UtcNow;
        return now
            .AddDays(-seedContext.Random.Next(0, 30))
            .AddHours(-seedContext.Random.Next(0, 24))
            .AddMinutes(-seedContext.Random.Next(0, 60));
    }

    private static void ValidateEngagement(
        IReadOnlyCollection<Reaction> reactions,
        IReadOnlyCollection<Comment> comments,
        IReadOnlyCollection<Repost> reposts,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Post> posts)
    {
        var postIds = posts.Select(p => p.Id).ToHashSet();
        var profileIds = profiles.Select(p => p.Id).ToHashSet();

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
        var repostIds = reposts.Select(r => r.Id).ToHashSet();

        if (reactions.Any(r => !profileIds.Contains(r.ProfileId)))
            throw new InvalidOperationException("Validation failed: reaction has invalid foreign key.");

        if (reactions.Any(r => r.PostId is not null && !postIds.Contains(r.PostId.Value)))
            throw new InvalidOperationException("Validation failed: reaction has invalid PostId foreign key.");

        if (reactions.Any(r => r.CommentId is not null && !commentIds.Contains(r.CommentId.Value)))
            throw new InvalidOperationException("Validation failed: reaction has invalid CommentId foreign key.");

        if (reactions.Any(r => r.RepostId is not null && !repostIds.Contains(r.RepostId.Value)))
            throw new InvalidOperationException("Validation failed: reaction has invalid RepostId foreign key.");

        if (comments.Any(c => c.ParentCommentId is not null && !commentIds.Contains(c.ParentCommentId.Value)))
            throw new InvalidOperationException("Validation failed: orphan comment detected.");

        var commentById = comments.ToDictionary(c => c.Id);
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
                $"{comment.Id},{comment.PostId},{comment.ProfileId},{comment.ParentCommentId},{EscapeCsv(comment.Content)},,{comment.CreatedAt:O},{comment.UpdatedAt:O}"));
        }

        return filePath;
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

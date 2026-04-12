using Favi_BE.Data;
using Favi_BE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Seed;

public sealed class SeedValidator
{
    public async Task ValidateAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var snapshot = TryLoadSeedSnapshot();

        await ValidateUsersAsync(db, snapshot, cancellationToken);
        await ValidateFollowsAsync(db, snapshot, cancellationToken);
        await ValidatePostsAndMediaAsync(db, snapshot, cancellationToken);
        await ValidateEngagementAsync(db, snapshot, cancellationToken);
        await ValidateTagsAsync(db, snapshot, cancellationToken);
        await ValidateNotificationsAsync(db, snapshot, cancellationToken);
    }

    private static async Task ValidateUsersAsync(AppDbContext db, SeedSnapshot snapshot, CancellationToken cancellationToken)
    {
        var scopedProfiles = snapshot.ProfileIds.Count > 0
            ? db.Profiles.Where(p => snapshot.ProfileIds.Contains(p.Id))
            : db.Profiles;

        var userCount = await scopedProfiles.CountAsync(cancellationToken);
        if (userCount < SeedConfig.Users.Min || userCount > SeedConfig.Users.Max)
            throw new InvalidOperationException("Seed validation failed: users count is outside expected range.");

        var duplicateUsernameExists = await scopedProfiles
            .GroupBy(p => p.Username.ToLower())
            .AnyAsync(g => g.Count() > 1, cancellationToken);
        if (duplicateUsernameExists)
            throw new InvalidOperationException("Seed validation failed: duplicate username detected.");

        var scopedEmailAccounts = snapshot.ProfileIds.Count > 0
            ? db.EmailAccounts.Where(e => snapshot.ProfileIds.Contains(e.Id))
            : db.EmailAccounts;

        var duplicateEmailExists = await scopedEmailAccounts
            .GroupBy(e => e.Email.ToLower())
            .AnyAsync(g => g.Count() > 1, cancellationToken);
        if (duplicateEmailExists)
            throw new InvalidOperationException("Seed validation failed: duplicate email detected.");

        var missingAvatarExists = await scopedProfiles
            .AnyAsync(p => string.IsNullOrWhiteSpace(p.AvatarUrl), cancellationToken);
        if (missingAvatarExists)
            throw new InvalidOperationException("Seed validation failed: profile avatar URL is empty.");

        var missingCoverExists = await scopedProfiles
            .AnyAsync(p => string.IsNullOrWhiteSpace(p.CoverUrl), cancellationToken);
        if (missingCoverExists)
            throw new InvalidOperationException("Seed validation failed: profile cover URL is empty.");

        var roleKinds = await scopedProfiles
            .Select(p => p.Role)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!roleKinds.Contains(UserRole.Admin)
            || !roleKinds.Contains(UserRole.Moderator)
            || !roleKinds.Contains(UserRole.User))
            throw new InvalidOperationException("Seed validation failed: account roles are not sufficiently varied.");
    }

    private static async Task ValidateFollowsAsync(AppDbContext db, SeedSnapshot snapshot, CancellationToken cancellationToken)
    {
        var userCount = snapshot.ProfileIds.Count > 0
            ? await db.Profiles.CountAsync(p => snapshot.ProfileIds.Contains(p.Id), cancellationToken)
            : await db.Profiles.CountAsync(cancellationToken);

        var feasibleMax = Math.Min(userCount * (userCount - 1), userCount * 20);
        var expectedMin = Math.Min(SeedConfig.Follows.Min, feasibleMax);
        var expectedMax = Math.Min(SeedConfig.Follows.Max, feasibleMax);

        var scopedFollows = snapshot.ProfileIds.Count > 0
            ? db.Follows.Where(f => snapshot.ProfileIds.Contains(f.FollowerId) && snapshot.ProfileIds.Contains(f.FolloweeId))
            : db.Follows;

        var followCount = await scopedFollows.CountAsync(cancellationToken);
        if (followCount < expectedMin || followCount > expectedMax)
            throw new InvalidOperationException("Seed validation failed: follows count is outside expected range.");

        var selfFollowExists = await scopedFollows
            .AnyAsync(f => f.FollowerId == f.FolloweeId, cancellationToken);
        if (selfFollowExists)
            throw new InvalidOperationException("Seed validation failed: self-follow detected.");

        var duplicateFollowExists = await scopedFollows
            .GroupBy(f => new { f.FollowerId, f.FolloweeId })
            .AnyAsync(g => g.Count() > 1, cancellationToken);
        if (duplicateFollowExists)
            throw new InvalidOperationException("Seed validation failed: duplicate follow edge detected.");

        var invalidFollowFkExists = await scopedFollows
            .AnyAsync(f => !db.Profiles.Any(p => p.Id == f.FollowerId) || !db.Profiles.Any(p => p.Id == f.FolloweeId), cancellationToken);
        if (invalidFollowFkExists)
            throw new InvalidOperationException("Seed validation failed: follow row has invalid foreign keys.");
    }

    private static async Task ValidatePostsAndMediaAsync(AppDbContext db, SeedSnapshot snapshot, CancellationToken cancellationToken)
    {
        var scopedPosts = snapshot.PostIds.Count > 0
            ? db.Posts.Where(p => snapshot.PostIds.Contains(p.Id))
            : db.Posts;

        var postCount = await scopedPosts.CountAsync(cancellationToken);
        if (postCount < SeedConfig.Posts.Min || postCount > SeedConfig.Posts.Max)
            throw new InvalidOperationException("Seed validation failed: posts count is outside expected range.");

        var postsWithoutMedia = await scopedPosts
            .Where(p => !db.PostMedias.Any(pm => pm.PostId == p.Id))
            .AnyAsync(cancellationToken);
        if (postsWithoutMedia)
            throw new InvalidOperationException("Seed validation failed: at least one post has no media.");

        var scopedPostMedias = snapshot.PostMediaIds.Count > 0
            ? db.PostMedias.Where(pm => snapshot.PostMediaIds.Contains(pm.Id))
            : db.PostMedias;

        var invalidMediaFkExists = await scopedPostMedias
            .Where(pm => pm.PostId != null)
            .AnyAsync(pm => !db.Posts.Any(p => p.Id == pm.PostId), cancellationToken);
        if (invalidMediaFkExists)
            throw new InvalidOperationException("Seed validation failed: post media has invalid PostId foreign key.");

        var emptyMediaUrlExists = await scopedPostMedias
            .AnyAsync(pm => string.IsNullOrWhiteSpace(pm.Url), cancellationToken);
        if (emptyMediaUrlExists)
            throw new InvalidOperationException("Seed validation failed: media URL is empty.");
    }

    private static async Task ValidateEngagementAsync(AppDbContext db, SeedSnapshot snapshot, CancellationToken cancellationToken)
    {
        var scopedReactions = snapshot.ReactionIds.Count > 0
            ? db.Reactions.Where(r => snapshot.ReactionIds.Contains(r.Id))
            : db.Reactions;

        var scopedComments = snapshot.CommentIds.Count > 0
            ? db.Comments.Where(c => snapshot.CommentIds.Contains(c.Id))
            : db.Comments;

        var scopedReposts = snapshot.RepostIds.Count > 0
            ? db.Reposts.Where(r => snapshot.RepostIds.Contains(r.Id))
            : db.Reposts;

        var reactionCount = await scopedReactions.CountAsync(cancellationToken);
        if (reactionCount < SeedConfig.Reactions.Min || reactionCount > SeedConfig.Reactions.Max)
            throw new InvalidOperationException("Seed validation failed: reactions count is outside expected range.");

        var commentCount = await scopedComments.CountAsync(cancellationToken);
        if (commentCount < SeedConfig.Comments.Min || commentCount > SeedConfig.Comments.Max)
            throw new InvalidOperationException("Seed validation failed: comments count is outside expected range.");

        var repostCount = await scopedReposts.CountAsync(cancellationToken);
        if (repostCount < SeedConfig.Reposts.Min || repostCount > SeedConfig.Reposts.Max)
            throw new InvalidOperationException("Seed validation failed: reposts count is outside expected range.");

        var duplicateReactionExists = await scopedReactions
            .GroupBy(r => new { r.PostId, r.ProfileId })
            .AnyAsync(g => g.Key.PostId != null && g.Count() > 1, cancellationToken);
        if (duplicateReactionExists)
            throw new InvalidOperationException("Seed validation failed: duplicate post reaction detected.");

        var duplicateCommentReactionExists = await scopedReactions
            .GroupBy(r => new { r.CommentId, r.ProfileId })
            .AnyAsync(g => g.Key.CommentId != null && g.Count() > 1, cancellationToken);
        if (duplicateCommentReactionExists)
            throw new InvalidOperationException("Seed validation failed: duplicate comment reaction detected.");

        var duplicateRepostReactionExists = await scopedReactions
            .GroupBy(r => new { r.RepostId, r.ProfileId })
            .AnyAsync(g => g.Key.RepostId != null && g.Count() > 1, cancellationToken);
        if (duplicateRepostReactionExists)
            throw new InvalidOperationException("Seed validation failed: duplicate repost reaction detected.");

        var reactionTargetInvalidExists = await scopedReactions
            .AnyAsync(r =>
                (r.PostId == null && r.CommentId == null && r.RepostId == null && r.CollectionId == null)
                || (r.PostId != null && (r.CommentId != null || r.RepostId != null || r.CollectionId != null))
                || (r.CommentId != null && (r.PostId != null || r.RepostId != null || r.CollectionId != null))
                || (r.RepostId != null && (r.PostId != null || r.CommentId != null || r.CollectionId != null))
                || (r.CollectionId != null && (r.PostId != null || r.CommentId != null || r.RepostId != null)),
                cancellationToken);
        if (reactionTargetInvalidExists)
            throw new InvalidOperationException("Seed validation failed: reaction target integrity is invalid.");

        var orphanCommentExists = await scopedComments
            .Where(c => c.ParentCommentId != null)
            .AnyAsync(c => !db.Comments.Any(parent => parent.Id == c.ParentCommentId), cancellationToken);
        if (orphanCommentExists)
            throw new InvalidOperationException("Seed validation failed: orphan comment detected.");

        var nestedDepthTooDeepExists = await scopedComments
            .Where(c => c.ParentCommentId != null)
            .AnyAsync(c => db.Comments.Any(parent => parent.Id == c.ParentCommentId && parent.ParentCommentId != null), cancellationToken);
        if (nestedDepthTooDeepExists)
            throw new InvalidOperationException("Seed validation failed: comment nesting exceeds two levels.");

        var crossPostParentChildExists = await scopedComments
            .Where(c => c.ParentCommentId != null)
            .AnyAsync(c => db.Comments.Any(parent => parent.Id == c.ParentCommentId && parent.PostId != c.PostId), cancellationToken);
        if (crossPostParentChildExists)
            throw new InvalidOperationException("Seed validation failed: parent/child comments belong to different posts.");

        var hasReplyComments = await scopedComments
            .AnyAsync(c => c.ParentCommentId != null, cancellationToken);
        if (!hasReplyComments)
            throw new InvalidOperationException("Seed validation failed: no reply comments generated.");

        var hasUrlInCommentContent = await scopedComments
            .AnyAsync(c => c.Content.Contains("http://") || c.Content.Contains("https://"), cancellationToken);
        if (!hasUrlInCommentContent)
            throw new InvalidOperationException("Seed validation failed: no comment contains URL.");

        var hasCommentReactions = await scopedReactions
            .AnyAsync(r => r.CommentId != null, cancellationToken);
        if (!hasCommentReactions)
            throw new InvalidOperationException("Seed validation failed: no reactions found for comments.");

        var duplicateRepostExists = await scopedReposts
            .GroupBy(r => new { r.ProfileId, r.OriginalPostId })
            .AnyAsync(g => g.Count() > 1, cancellationToken);
        if (duplicateRepostExists)
            throw new InvalidOperationException("Seed validation failed: duplicate repost pair detected.");
    }

    private static async Task ValidateTagsAsync(AppDbContext db, SeedSnapshot snapshot, CancellationToken cancellationToken)
    {
        var scopedTags = snapshot.TagIds.Count > 0
            ? db.Tags.Where(t => snapshot.TagIds.Contains(t.Id))
            : db.Tags;

        var scopedPostTags = snapshot.PostTagPairs.Count > 0
            ? db.PostTags.Where(pt => snapshot.PostIds.Contains(pt.PostId) && snapshot.TagIds.Contains(pt.TagId))
            : db.PostTags;

        var tagCount = await scopedTags.CountAsync(cancellationToken);
        if (tagCount < SeedConfig.Tags.Min || tagCount > SeedConfig.Tags.Max)
            throw new InvalidOperationException("Seed validation failed: tags count is outside expected range.");

        var duplicateTagExists = await scopedTags
            .GroupBy(t => t.Name.ToLower())
            .AnyAsync(g => g.Count() > 1, cancellationToken);
        if (duplicateTagExists)
            throw new InvalidOperationException("Seed validation failed: duplicate tag name detected.");

        var duplicatePostTagExists = await scopedPostTags
            .GroupBy(pt => new { pt.PostId, pt.TagId })
            .AnyAsync(g => g.Count() > 1, cancellationToken);
        if (duplicatePostTagExists)
            throw new InvalidOperationException("Seed validation failed: duplicate post-tag pair detected.");

        var invalidPostTagFkExists = await scopedPostTags
            .AnyAsync(pt => !db.Posts.Any(p => p.Id == pt.PostId) || !db.Tags.Any(t => t.Id == pt.TagId), cancellationToken);
        if (invalidPostTagFkExists)
            throw new InvalidOperationException("Seed validation failed: post-tag row has invalid foreign keys.");

        var scopedPosts = snapshot.PostIds.Count > 0
            ? db.Posts.Where(p => snapshot.PostIds.Contains(p.Id))
            : db.Posts;

        var postsWithoutTagCount = await scopedPosts
            .CountAsync(p => !db.PostTags.Any(pt => pt.PostId == p.Id), cancellationToken);

        if (postsWithoutTagCount > 0)
            Console.WriteLine($"[SeedValidator] WARNING: {postsWithoutTagCount} posts have no tag.");
    }

    private static async Task ValidateNotificationsAsync(AppDbContext db, SeedSnapshot snapshot, CancellationToken cancellationToken)
    {
        var scopedNotifications = snapshot.NotificationIds.Count > 0
            ? db.Notifications.Where(n => snapshot.NotificationIds.Contains(n.Id))
            : db.Notifications;

        var notificationsExist = await scopedNotifications.AnyAsync(cancellationToken);
        if (!notificationsExist)
            return;

        var invalidActorOrRecipient = await scopedNotifications
            .AnyAsync(n => n.ActorProfileId == n.RecipientProfileId
                           || !db.Profiles.Any(p => p.Id == n.ActorProfileId)
                           || !db.Profiles.Any(p => p.Id == n.RecipientProfileId), cancellationToken);
        if (invalidActorOrRecipient)
            throw new InvalidOperationException("Seed validation failed: notification actor/recipient is invalid.");

        var invalidTargetPost = await scopedNotifications
            .Where(n => n.TargetPostId != null)
            .AnyAsync(n => !db.Posts.Any(p => p.Id == n.TargetPostId), cancellationToken);
        if (invalidTargetPost)
            throw new InvalidOperationException("Seed validation failed: notification has invalid target post.");

        var invalidTargetComment = await scopedNotifications
            .Where(n => n.TargetCommentId != null)
            .AnyAsync(n => !db.Comments.Any(c => c.Id == n.TargetCommentId), cancellationToken);
        if (invalidTargetComment)
            throw new InvalidOperationException("Seed validation failed: notification has invalid target comment.");
    }

    private sealed class SeedSnapshot
    {
        public HashSet<Guid> ProfileIds { get; init; } = [];
        public HashSet<Guid> PostIds { get; init; } = [];
        public HashSet<Guid> PostMediaIds { get; init; } = [];
        public HashSet<Guid> ReactionIds { get; init; } = [];
        public HashSet<Guid> CommentIds { get; init; } = [];
        public HashSet<Guid> RepostIds { get; init; } = [];
        public HashSet<Guid> TagIds { get; init; } = [];
        public HashSet<(Guid PostId, Guid TagId)> PostTagPairs { get; init; } = [];
        public HashSet<Guid> NotificationIds { get; init; } = [];
    }

    private static SeedSnapshot TryLoadSeedSnapshot()
    {
        var rootCandidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "seed-output"),
            Path.Combine(AppContext.BaseDirectory, "seed-output"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "seed-output"))
        };

        var seedRoot = rootCandidates.FirstOrDefault(Directory.Exists);
        if (string.IsNullOrWhiteSpace(seedRoot))
            return new SeedSnapshot();

        return new SeedSnapshot
        {
            ProfileIds = LoadGuidColumn(Path.Combine(seedRoot, "users.csv"), "profile_id"),
            PostIds = LoadGuidColumn(Path.Combine(seedRoot, "posts.csv"), "post_id"),
            PostMediaIds = LoadGuidColumn(Path.Combine(seedRoot, "post-medias.csv"), "media_id"),
            ReactionIds = LoadGuidColumn(Path.Combine(seedRoot, "reactions.csv"), "reaction_id"),
            CommentIds = LoadGuidColumn(Path.Combine(seedRoot, "comments.csv"), "comment_id"),
            RepostIds = LoadGuidColumn(Path.Combine(seedRoot, "reposts.csv"), "repost_id"),
            TagIds = LoadGuidColumn(Path.Combine(seedRoot, "tags.csv"), "tag_id"),
            PostTagPairs = LoadGuidPair(Path.Combine(seedRoot, "post-tags.csv"), "post_id", "tag_id"),
            NotificationIds = LoadGuidColumn(Path.Combine(seedRoot, "notifications.csv"), "notification_id")
        };
    }

    private static HashSet<Guid> LoadGuidColumn(string filePath, string columnName)
    {
        var result = new HashSet<Guid>();
        if (!File.Exists(filePath))
            return result;

        var lines = File.ReadLines(filePath).ToList();
        if (lines.Count <= 1)
            return result;

        var headers = lines[0].Split(',');
        var index = Array.FindIndex(headers, h => string.Equals(h.Trim(), columnName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return result;

        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length <= index)
                continue;

            if (Guid.TryParse(parts[index].Trim(), out var id))
                result.Add(id);
        }

        return result;
    }

    private static HashSet<(Guid First, Guid Second)> LoadGuidPair(string filePath, string firstColumn, string secondColumn)
    {
        var result = new HashSet<(Guid First, Guid Second)>();
        if (!File.Exists(filePath))
            return result;

        var lines = File.ReadLines(filePath).ToList();
        if (lines.Count <= 1)
            return result;

        var headers = lines[0].Split(',');
        var firstIndex = Array.FindIndex(headers, h => string.Equals(h.Trim(), firstColumn, StringComparison.OrdinalIgnoreCase));
        var secondIndex = Array.FindIndex(headers, h => string.Equals(h.Trim(), secondColumn, StringComparison.OrdinalIgnoreCase));
        if (firstIndex < 0 || secondIndex < 0)
            return result;

        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length <= Math.Max(firstIndex, secondIndex))
                continue;

            if (Guid.TryParse(parts[firstIndex].Trim(), out var first) && Guid.TryParse(parts[secondIndex].Trim(), out var second))
                result.Add((first, second));
        }

        return result;
    }
}

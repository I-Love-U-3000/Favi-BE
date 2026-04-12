using System.Globalization;
using System.Text;
using Favi_BE.API.Models.Entities;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedNotificationsStep
{
    private const int MaxNotifications = 8000;

    public async Task<SeedNotificationsResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Post> posts,
        IReadOnlyList<Follow> follows,
        IReadOnlyList<Reaction> reactions,
        IReadOnlyList<Comment> comments,
        IReadOnlyList<Repost> reposts,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        var notifications = GenerateNotifications(posts, follows, reactions, comments, reposts, seedContext);

        ValidateNotifications(notifications, posts, comments);

        if (notifications.Count > 0)
        {
            await db.Notifications.AddRangeAsync(notifications, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        var exportPath = ExportNotificationsCsv(notifications);
        return new SeedNotificationsResult(notifications.Count, exportPath);
    }

    private static List<Notification> GenerateNotifications(
        IReadOnlyList<Post> posts,
        IReadOnlyList<Follow> follows,
        IReadOnlyList<Reaction> reactions,
        IReadOnlyList<Comment> comments,
        IReadOnlyList<Repost> reposts,
        SeedContext seedContext)
    {
        var postOwnerMap = posts.ToDictionary(p => p.Id, p => p.ProfileId);
        var notifications = new List<Notification>();
        var dedupe = new HashSet<(NotificationType Type, Guid Recipient, Guid Actor, Guid? PostId, Guid? CommentId)>();

        foreach (var follow in follows)
        {
            if (notifications.Count >= MaxNotifications) break;
            if (seedContext.Random.NextDouble() > 0.05) continue;
            if (follow.FollowerId == follow.FolloweeId) continue;

            var key = (NotificationType.Follow, follow.FolloweeId, follow.FollowerId, (Guid?)null, (Guid?)null);
            if (!dedupe.Add(key)) continue;

            notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Follow,
                RecipientProfileId = follow.FolloweeId,
                ActorProfileId = follow.FollowerId,
                Message = "started following you",
                IsRead = false,
                CreatedAt = follow.CreatedAt
            });
        }

        foreach (var reaction in reactions)
        {
            if (notifications.Count >= MaxNotifications) break;
            if (reaction.PostId is null) continue;
            if (seedContext.Random.NextDouble() > 0.08) continue;
            if (!postOwnerMap.TryGetValue(reaction.PostId.Value, out var recipientId)) continue;
            if (recipientId == reaction.ProfileId) continue;

            var key = (NotificationType.Like, recipientId, reaction.ProfileId, reaction.PostId, (Guid?)null);
            if (!dedupe.Add(key)) continue;

            notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Like,
                RecipientProfileId = recipientId,
                ActorProfileId = reaction.ProfileId,
                TargetPostId = reaction.PostId,
                Message = "reacted to your post",
                IsRead = false,
                CreatedAt = reaction.CreatedAt
            });
        }

        foreach (var comment in comments)
        {
            if (notifications.Count >= MaxNotifications) break;
            if (seedContext.Random.NextDouble() > 0.15) continue;
            if (!postOwnerMap.TryGetValue(comment.PostId, out var recipientId)) continue;
            if (recipientId == comment.ProfileId) continue;

            var key = (NotificationType.Comment, recipientId, comment.ProfileId, (Guid?)comment.PostId, comment.Id);
            if (!dedupe.Add(key)) continue;

            notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Comment,
                RecipientProfileId = recipientId,
                ActorProfileId = comment.ProfileId,
                TargetPostId = comment.PostId,
                TargetCommentId = comment.Id,
                Message = "commented on your post",
                IsRead = false,
                CreatedAt = comment.CreatedAt
            });
        }

        foreach (var repost in reposts)
        {
            if (notifications.Count >= MaxNotifications) break;
            if (seedContext.Random.NextDouble() > 0.20) continue;
            if (!postOwnerMap.TryGetValue(repost.OriginalPostId, out var recipientId)) continue;
            if (recipientId == repost.ProfileId) continue;

            var key = (NotificationType.Share, recipientId, repost.ProfileId, repost.OriginalPostId, (Guid?)null);
            if (!dedupe.Add(key)) continue;

            notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Share,
                RecipientProfileId = recipientId,
                ActorProfileId = repost.ProfileId,
                TargetPostId = repost.OriginalPostId,
                Message = "shared your post",
                IsRead = false,
                CreatedAt = repost.CreatedAt
            });
        }

        return notifications.OrderBy(n => n.CreatedAt).ToList();
    }

    private static void ValidateNotifications(
        IReadOnlyCollection<Notification> notifications,
        IReadOnlyList<Post> posts,
        IReadOnlyList<Comment> comments)
    {
        var postIds = posts.Select(p => p.Id).ToHashSet();
        var commentIds = comments.Select(c => c.Id).ToHashSet();

        if (notifications.Any(n => n.RecipientProfileId == n.ActorProfileId))
            throw new InvalidOperationException("Validation failed: notification has same recipient and actor.");

        if (notifications.Any(n => string.IsNullOrWhiteSpace(n.Message)))
            throw new InvalidOperationException("Validation failed: notification message is empty.");

        if (notifications.Any(n => n.TargetPostId is not null && !postIds.Contains(n.TargetPostId.Value)))
            throw new InvalidOperationException("Validation failed: notification has invalid TargetPostId.");

        if (notifications.Any(n => n.TargetCommentId is not null && !commentIds.Contains(n.TargetCommentId.Value)))
            throw new InvalidOperationException("Validation failed: notification has invalid TargetCommentId.");
    }

    private static string ExportNotificationsCsv(IEnumerable<Notification> notifications)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var filePath = Path.Combine(outputRoot, "notifications.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("notification_id,type,recipient_profile_id,actor_profile_id,target_post_id,target_comment_id,message,is_read,created_at");
        foreach (var notification in notifications)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{notification.Id},{notification.Type},{notification.RecipientProfileId},{notification.ActorProfileId},{notification.TargetPostId},{notification.TargetCommentId},{EscapeCsv(notification.Message)},{notification.IsRead},{notification.CreatedAt:O}"));
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

public readonly record struct SeedNotificationsResult(int CreatedNotifications, string ExportPath);

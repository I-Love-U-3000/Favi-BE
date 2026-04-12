using Favi_BE.API.Seed.Steps;
using Favi_BE.Data;
using Favi_BE.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Favi_BE.API.Seed;

public static class SeedPipeline
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Log("[SeedPipeline] Starting deterministic seed pipeline...");

        var seedContext = new SeedContext(SeedConfig.SeedKey);

        var hasExistingProfiles = await db.Profiles.AnyAsync(cancellationToken);
        var hasExistingEmailAccounts = await db.EmailAccounts.AnyAsync(cancellationToken);

        Log("[SeedPipeline] Running Step 1 - Seed Users / Profiles...");
        if (!hasExistingProfiles && !hasExistingEmailAccounts)
        {
            var step1 = new SeedUsersStep();
            var step1Result = await step1.ExecuteAsync(db, seedContext, cancellationToken);
            Log($"[SeedPipeline] Step 1 done. Created users: {step1Result.CreatedCount}");
            Log($"[SeedPipeline] Step 1 export: {step1Result.ExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 1 skipped: users already exist.");
        }

        var profiles = await db.Profiles
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        if (profiles.Count < 2)
        {
            Log("[SeedPipeline] Step 2 skipped: profiles are missing. Implement/run Step 1 first.", "WARN");
            return;
        }

        Log("[SeedPipeline] Running Step 2 - Seed Social Graph (Follows)...");

        if (!await db.Follows.AnyAsync(cancellationToken))
        {
            var step2 = new SeedFollowsStep();
            var result = await step2.ExecuteAsync(db, profiles, seedContext, cancellationToken);

            Log($"[SeedPipeline] Step 2 done. Created follows: {result.CreatedCount}");
            Log($"[SeedPipeline] Step 2 export: {result.ExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 2 skipped: follows already exist.");
        }

        Log("[SeedPipeline] Running Step 3 - Seed Posts + Media...");

        if (!await db.Posts.AnyAsync(cancellationToken) && !await db.PostMedias.AnyAsync(cancellationToken))
        {
            var step3 = new SeedPostsStep();
            var step3Result = await step3.ExecuteAsync(db, profiles, seedContext, cancellationToken);
            Log($"[SeedPipeline] Step 3 done. Posts: {step3Result.CreatedPosts}, PostMedias: {step3Result.CreatedPostMedias}");
            Log($"[SeedPipeline] Step 3 exports: {step3Result.PostsExportPath} | {step3Result.PostMediasExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 3 skipped: posts/post-medias already exist.");
        }

        Log("[SeedPipeline] Running Step 4 - Seed Engagement...");
        var posts = await db.Posts
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        if (posts.Count == 0)
            throw new InvalidOperationException("Step 4 requires posts from Step 3.");

        if (!await db.Reactions.AnyAsync(cancellationToken)
            && !await db.Comments.AnyAsync(cancellationToken)
            && !await db.Reposts.AnyAsync(cancellationToken))
        {
            var step4 = new SeedEngagementStep();
            var step4Result = await step4.ExecuteAsync(db, profiles, posts, seedContext, cancellationToken);
            Log($"[SeedPipeline] Step 4 done. Reactions: {step4Result.CreatedReactions}, Comments: {step4Result.CreatedComments}, Reposts: {step4Result.CreatedReposts}");
            Log($"[SeedPipeline] Step 4 exports: {step4Result.ReactionsExportPath} | {step4Result.CommentsExportPath} | {step4Result.RepostsExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 4 skipped: engagement data already exists.");
        }

        Log("[SeedPipeline] Running Step 5 - Seed Tags + PostTags...");
        if (!await db.Tags.AnyAsync(cancellationToken) && !await db.PostTags.AnyAsync(cancellationToken))
        {
            var step5 = new SeedTagsStep();
            var step5Result = await step5.ExecuteAsync(db, posts, seedContext, cancellationToken);
            Log($"[SeedPipeline] Step 5 done. Tags: {step5Result.CreatedTags}, PostTags: {step5Result.CreatedPostTags}");
            Log($"[SeedPipeline] Step 5 exports: {step5Result.TagsExportPath} | {step5Result.PostTagsExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 5 skipped: tags/post-tags already exist.");
        }

        Log("[SeedPipeline] Running Step 6 - Seed Lightweight Notifications...");
        if (!await db.Notifications.AnyAsync(cancellationToken))
        {
            var follows = await db.Follows
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var reactions = await db.Reactions
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var comments = await db.Comments
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var reposts = await db.Reposts
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var step6 = new SeedNotificationsStep();
            var step6Result = await step6.ExecuteAsync(
                db,
                posts,
                follows,
                reactions,
                comments,
                reposts,
                seedContext,
                cancellationToken);

            Log($"[SeedPipeline] Step 6 done. Notifications: {step6Result.CreatedNotifications}");
            Log($"[SeedPipeline] Step 6 export: {step6Result.ExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 6 skipped: notifications already exist.");
        }

        Log("[SeedPipeline] Running Step 7 - Global Validation Gate...");
        var validator = new SeedValidator();
        await validator.ValidateAsync(db, cancellationToken);
        Log("[SeedPipeline] Step 7 done. Validation passed.");

        Log("[SeedPipeline] Running auth bootstrap for tokens.csv...");
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        var authBootstrap = new SeedAuthBootstrapStep();
        var authResult = await authBootstrap.ExecuteAsync(db, jwtService, cancellationToken);
        Log($"[SeedPipeline] Auth bootstrap done. Tokens generated for users: {authResult.UserCount}");
        Log($"[SeedPipeline] Auth bootstrap export: {authResult.ExportPath}");

        Log("[SeedPipeline] Running Step 8 - Export Dataset...");
        var step8 = new SeedExport();
        var step8Result = await step8.ExecuteAsync(db, cancellationToken);
        Log($"[SeedPipeline] Step 8 done. Output root: {step8Result.OutputRoot}");
        Log($"[SeedPipeline] Step 8 manifest: {step8Result.ManifestPath}");
    }

    private static void Log(string message, string level = "INFO")
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            Level = level,
            Message = message
        };

        var logLine = JsonSerializer.Serialize(logEntry);
        Console.WriteLine(logLine);

        var logFilePath = Path.Combine("seed-output", "pipeline.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        File.AppendAllText(logFilePath, logLine + Environment.NewLine);
    }
}

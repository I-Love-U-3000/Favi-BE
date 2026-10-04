using Favi_BE.API.Seed.Steps;
using Favi_BE.Data;
using Favi_BE.Interfaces.Services;
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

        var anyStepExecuted = false;
        var hasExistingProfiles = await db.Profiles.AnyAsync(cancellationToken);
        var hasExistingEmailAccounts = await db.EmailAccounts.AnyAsync(cancellationToken);

        Log("[SeedPipeline] Running Step 1 - Seed Users / Profiles...");
        if (!hasExistingProfiles && !hasExistingEmailAccounts)
        {
            anyStepExecuted = true;
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
            anyStepExecuted = true;
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
            anyStepExecuted = true;
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

        var hasReactions = await db.Reactions.AnyAsync(cancellationToken);
        var hasComments = await db.Comments.AnyAsync(cancellationToken);
        var hasReposts = await db.Reposts.AnyAsync(cancellationToken);

        if (!hasReactions || !hasComments || !hasReposts)
        {
            if (hasReactions || hasComments || hasReposts)
            {
                Log("[SeedPipeline] Incomplete engagement detected from previous run. Resetting engagement tables...", "WARN");
                if (db.Database.IsRelational())
                {
                    await db.Database.ExecuteSqlRawAsync(@"TRUNCATE TABLE ""Reactions"", ""Comments"", ""Reposts"" CASCADE;", cancellationToken);
                }
                else
                {
                    db.Reactions.RemoveRange(db.Reactions);
                    db.Comments.RemoveRange(db.Comments);
                    db.Reposts.RemoveRange(db.Reposts);
                    await db.SaveChangesAsync(cancellationToken);
                }
            }

            anyStepExecuted = true;
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
            anyStepExecuted = true;
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
            anyStepExecuted = true;
            var follows = await db.Follows
                .AsNoTracking()
                .OrderBy(f => f.FollowerId)
                .ThenBy(f => f.FolloweeId)
                .Take(30000)
                .ToListAsync(cancellationToken);
            var reactions = await db.Reactions
                .AsNoTracking()
                .Where(r => r.PostId != null)
                .OrderBy(r => r.Id)
                .Take(30000)
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

        Log("[SeedPipeline] Running Step 7 - Seed Stories...");
        if (!await db.Stories.AnyAsync(cancellationToken))
        {
            anyStepExecuted = true;
            var step7 = new SeedStoriesStep();
            var step7Result = await step7.ExecuteAsync(db, profiles, seedContext, cancellationToken);
            Log($"[SeedPipeline] Step 7 done. Stories: {step7Result.CreatedStories}");
            Log($"[SeedPipeline] Step 7 export: {step7Result.ExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 7 skipped: stories already exist.");
        }

        Log("[SeedPipeline] Running Step 7b - Seed Collections...");
        if (!await db.Collections.AnyAsync(cancellationToken))
        {
            anyStepExecuted = true;
            var step7b = new SeedCollectionsStep();
            var step7bResult = await step7b.ExecuteAsync(db, profiles, posts, seedContext, cancellationToken);
            Log($"[SeedPipeline] Step 7b done. Collections: {step7bResult.CreatedCollections}, PostCollections: {step7bResult.CreatedPostCollections}");
            Log($"[SeedPipeline] Step 7b export: {step7bResult.CollectionsExportPath} | {step7bResult.PostCollectionsExportPath}");
        }
        else
        {
            Log("[SeedPipeline] Step 7b skipped: collections already exist.");
        }

        if (!anyStepExecuted)
        {
            Log("[SeedPipeline] All seed steps skipped: database is already populated.");
            Log("[SeedPipeline] Skipping Step 9 (Global Validation Gate) and Step 10 (Export Dataset) on already-seeded database.");
            TriggerBackgroundVectorIndex(serviceProvider);
            return;
        }

        Log("[SeedPipeline] Running Step 9 - Global Validation Gate...");
        var validator = new SeedValidator();
        await validator.ValidateAsync(db, cancellationToken);
        Log("[SeedPipeline] Step 9 done. Validation passed.");

        Log("[SeedPipeline] Running auth bootstrap for tokens.csv...");
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        var authBootstrap = new SeedAuthBootstrapStep();
        var authResult = await authBootstrap.ExecuteAsync(db, jwtService, cancellationToken);
        Log($"[SeedPipeline] Auth bootstrap done. Tokens generated for users: {authResult.UserCount}");
        Log($"[SeedPipeline] Auth bootstrap export: {authResult.ExportPath}");

        Log("[SeedPipeline] Running Step 10 - Export Dataset...");
        var step10 = new SeedExport();
        var step10Result = await step10.ExecuteAsync(db, cancellationToken);
        Log($"[SeedPipeline] Step 10 done. Output root: {step10Result.OutputRoot}");
        Log($"[SeedPipeline] Step 10 manifest: {step10Result.ManifestPath}");

        // Step 8: Trigger vector indexing in the background so Kestrel opens port immediately
        TriggerBackgroundVectorIndex(serviceProvider);
    }

    private static void TriggerBackgroundVectorIndex(IServiceProvider serviceProvider)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var bgScope = serviceProvider.CreateScope();
                var bgDb = bgScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var vectorIndexService = bgScope.ServiceProvider.GetRequiredService<IVectorIndexService>();
                var seedContext = new SeedContext(SeedConfig.SeedKey);

                Log("[SeedPipeline:Background] Running Step 8 - Background Seed Vector Index (Qdrant)...");
                var vectorIndexStep = new SeedVectorIndexStep();
                var result = await vectorIndexStep.ExecuteAsync(bgDb, vectorIndexService, seedContext, CancellationToken.None);
                Log($"[SeedPipeline:Background] Step 8 done. Indexed posts: {result.IndexedCount} in {result.ElapsedMilliseconds}ms. Manifest: {result.ManifestPath}");
            }
            catch (Exception ex)
            {
                Log($"[SeedPipeline:Background] Step 8 background indexing error: {ex.Message}", "ERROR");
            }
        });
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

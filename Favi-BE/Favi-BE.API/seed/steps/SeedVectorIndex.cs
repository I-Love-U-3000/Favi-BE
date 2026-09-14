using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Favi_BE.Data;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedVectorIndexStep
{
    public async Task<SeedVectorIndexResult> ExecuteAsync(
        AppDbContext db,
        IVectorIndexService vectorIndexService,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var manifestCandidates = new[]
        {
            Path.Combine(outputRoot, "vector-index-manifest.json"),
            Path.Combine(outputRoot, "seed-output", "vector-index-manifest.json"),
            Path.Combine(AppContext.BaseDirectory, "seed-output", "vector-index-manifest.json"),
            Path.Combine(AppContext.BaseDirectory, "seed-output", "seed-output", "vector-index-manifest.json")
        };
        var manifestPath = manifestCandidates.FirstOrDefault(File.Exists) ?? Path.Combine(outputRoot, "vector-index-manifest.json");

        if (File.Exists(manifestPath))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(manifestPath));
                if (existing.TryGetProperty("Status", out var statusProp) && statusProp.GetString() == "Completed")
                {
                    var count = existing.TryGetProperty("IndexedCount", out var cProp) ? cProp.GetInt32() : 0;
                    return new SeedVectorIndexResult(count, 0, manifestPath);
                }
            }
            catch { }
        }

        if (!vectorIndexService.IsEnabled())
        {
            var disabledManifest = new
            {
                Status = "Skipped",
                Reason = "VectorIndexService is disabled or unavailable in configuration",
                IndexedCount = 0,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(disabledManifest, new JsonSerializerOptions { WriteIndented = true }));
            return new SeedVectorIndexResult(0, 0, manifestPath);
        }

        var targetCount = seedContext.Random.Next(SeedConfig.VectorizedPosts.Min, SeedConfig.VectorizedPosts.Max + 1);

        var eligiblePosts = await db.Posts
            .AsNoTracking()
            .Include(p => p.PostMedias)
            .Where(p => p.PostMedias.Any(pm => !string.IsNullOrEmpty(pm.Url)))
            .OrderBy(p => p.CreatedAt)
            .Take(targetCount)
            .ToListAsync(cancellationToken);

        if (eligiblePosts.Count == 0)
        {
            var emptyManifest = new
            {
                Status = "Skipped",
                Reason = "No eligible posts with media found",
                IndexedCount = 0,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(emptyManifest, new JsonSerializerOptions { WriteIndented = true }));
            return new SeedVectorIndexResult(0, 0, manifestPath);
        }

        const int batchSize = 100;
        var totalIndexed = 0;
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < eligiblePosts.Count; i += batchSize)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var batch = eligiblePosts.Skip(i).Take(batchSize).ToList();
            var indexedInBatch = await vectorIndexService.IndexPostsBatchAsync(batch, cancellationToken);
            totalIndexed += indexedInBatch;
            if (i + batchSize < eligiblePosts.Count)
            {
                await Task.Delay(50, cancellationToken);
            }
        }

        sw.Stop();

        var manifest = new
        {
            Status = "Completed",
            TargetCount = targetCount,
            EligibleCount = eligiblePosts.Count,
            IndexedCount = totalIndexed,
            ElapsedMilliseconds = sw.ElapsedMilliseconds,
            AverageThroughputPostsPerSecond = sw.ElapsedMilliseconds > 0 ? (totalIndexed / (sw.ElapsedMilliseconds / 1000.0)) : 0,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

        return new SeedVectorIndexResult(totalIndexed, sw.ElapsedMilliseconds, manifestPath);
    }
}

public readonly record struct SeedVectorIndexResult(int IndexedCount, long ElapsedMilliseconds, string ManifestPath);

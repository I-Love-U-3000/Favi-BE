using System.Text.Json;
using Favi_BE.Data;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Seed;

public sealed class SeedExport
{
    public async Task<SeedExportResult> ExecuteAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var expectedCoreFiles = new[]
        {
            "users.csv",
            "tokens.csv",
            "follows.csv",
            "posts.csv",
            "post-medias.csv",
            "reactions.csv",
            "comments.csv",
            "reposts.csv",
            "tags.csv",
            "post-tags.csv",
            SeedConfig.OutputPaths.ImageCatalogFileName,
            SeedConfig.OutputPaths.RunImageSetFileName
        };

        var missingCoreFiles = expectedCoreFiles
            .Where(file => !File.Exists(Path.Combine(outputRoot, file)))
            .ToList();

        if (missingCoreFiles.Count > 0)
        {
            throw new InvalidOperationException(
                $"Step 8 failed: missing core export artifacts: {string.Join(", ", missingCoreFiles)}");
        }

        var manifestPath = Path.Combine(outputRoot, SeedConfig.OutputPaths.ManifestFileName);

        var manifest = new RuntimeSeedManifest
        {
            SeedKey = SeedConfig.SeedKey,
            GeneratedAt = DateTime.UtcNow,
            Counts = new RuntimeSeedCounts
            {
                Users = await db.Profiles.CountAsync(cancellationToken),
                Posts = await db.Posts.CountAsync(cancellationToken),
                Follows = await db.Follows.CountAsync(cancellationToken),
                Reactions = await db.Reactions.CountAsync(cancellationToken),
                Comments = await db.Comments.CountAsync(cancellationToken),
                Reposts = await db.Reposts.CountAsync(cancellationToken),
                Tags = await db.Tags.CountAsync(cancellationToken),
                Notifications = await db.Notifications.CountAsync(cancellationToken)
            },
            Artifacts = expectedCoreFiles
                .Concat([SeedConfig.OutputPaths.ManifestFileName])
                .ToArray(),
            OptionalArtifacts = GetExistingOptionalArtifacts(outputRoot)
        };

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(manifestPath, json, cancellationToken);

        return new SeedExportResult(outputRoot, manifestPath, manifest.Counts);
    }

    private static string[] GetExistingOptionalArtifacts(string outputRoot)
    {
        var optionalFiles = new[]
        {
            "notifications.csv"
        };

        return optionalFiles
            .Where(file => File.Exists(Path.Combine(outputRoot, file)))
            .ToArray();
    }

    public sealed class RuntimeSeedManifest
    {
        public required string SeedKey { get; init; }
        public required DateTime GeneratedAt { get; init; }
        public required RuntimeSeedCounts Counts { get; init; }
        public required string[] Artifacts { get; init; }
        public required string[] OptionalArtifacts { get; init; }
    }

    public sealed class RuntimeSeedCounts
    {
        public int Users { get; init; }
        public int Posts { get; init; }
        public int Follows { get; init; }
        public int Reactions { get; init; }
        public int Comments { get; init; }
        public int Reposts { get; init; }
        public int Tags { get; init; }
        public int Notifications { get; init; }
    }
}

public readonly record struct SeedExportResult(
    string OutputRoot,
    string ManifestPath,
    SeedExport.RuntimeSeedCounts Counts);

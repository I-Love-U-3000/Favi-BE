using System.Globalization;
using System.Text;
using System.Text.Json;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedCollectionsStep
{
    public async Task<SeedCollectionsResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Profile> profiles,
        IReadOnlyList<Post> posts,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (profiles.Count == 0 || posts.Count == 0)
            throw new InvalidOperationException("Step requires profiles and posts.");

        var catalog = TryLoadRealCollectionsCatalog();
        var targetCount = catalog != null && catalog.Count > 0 
            ? catalog.Count 
            : seedContext.Random.Next(SeedConfig.Collections.Min, SeedConfig.Collections.Max + 1);

        var eligibleProfiles = profiles
            .Where(p => InferActivityRole(p) != "lurker")
            .ToList();

        if (eligibleProfiles.Count == 0)
            eligibleProfiles = profiles.ToList();

        var collections = new List<Collection>(targetCount);
        var postCollections = new List<PostCollection>();
        var now = DateTime.UtcNow;

        for (var i = 0; i < targetCount; i++)
        {
            var profile = eligibleProfiles[i % eligibleProfiles.Count];
            var collId = Guid.NewGuid();
            var createdAt = now.AddDays(-seedContext.Random.Next(1, 30));

            string title;
            string? description;
            string? coverUrl;

            if (catalog != null && catalog.Count > 0)
            {
                var item = catalog[i % catalog.Count];
                title = item.Title;
                description = item.Description;
                coverUrl = item.CoverUrl;
            }
            else
            {
                title = $"Collection #{i + 1}";
                description = "Curated collection of interesting posts.";
                coverUrl = $"https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&h=600&fit=crop&q=80";
            }

            var collection = new Collection
            {
                Id = collId,
                ProfileId = profile.Id,
                Title = title,
                Description = description,
                CoverImageUrl = coverUrl,
                CoverImagePublicId = $"seed/collection/{collId:N}",
                PrivacyLevel = PrivacyLevel.Public,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            collections.Add(collection);

            // Attach posts: ensure at least 2 collections have 1000+ posts
            int postCount;
            int startIndex;
            if (i == 0)
            {
                postCount = Math.Min(1050, posts.Count);
                startIndex = 0;
            }
            else if (i == 1)
            {
                postCount = Math.Min(1020, posts.Count);
                startIndex = 500 % posts.Count;
            }
            else
            {
                postCount = seedContext.Random.Next(3, 9);
                startIndex = (i * 7) % posts.Count;
            }

            for (var p = 0; p < postCount; p++)
            {
                var post = posts[(startIndex + p) % posts.Count];
                postCollections.Add(new PostCollection
                {
                    CollectionId = collId,
                    PostId = post.Id
                });
            }
        }

        // Generate reactions to collections: ensure at least 2 collections have 1000+ reactions
        var collectionReactions = new List<Reaction>();
        if (collections.Count >= 2)
        {
            var coll0Reactors = profiles
                .Where(p => p.Id != collections[0].ProfileId)
                .Take(1050)
                .ToList();
            foreach (var rProfile in coll0Reactors)
            {
                collectionReactions.Add(new Reaction
                {
                    Id = Guid.NewGuid(),
                    CollectionId = collections[0].Id,
                    ProfileId = rProfile.Id,
                    Type = ReactionType.Like,
                    CreatedAt = collections[0].CreatedAt.AddMinutes(seedContext.Random.Next(1, 1440))
                });
            }

            var coll1Reactors = profiles
                .Where(p => p.Id != collections[1].ProfileId)
                .Take(1020)
                .ToList();
            foreach (var rProfile in coll1Reactors)
            {
                collectionReactions.Add(new Reaction
                {
                    Id = Guid.NewGuid(),
                    CollectionId = collections[1].Id,
                    ProfileId = rProfile.Id,
                    Type = ReactionType.Like,
                    CreatedAt = collections[1].CreatedAt.AddMinutes(seedContext.Random.Next(1, 1440))
                });
            }
        }

        // Validate collections high performance requirement
        if (collections.Count >= 2)
        {
            var coll0PostCount = postCollections.Count(pc => pc.CollectionId == collections[0].Id);
            var coll0RxCount = collectionReactions.Count(r => r.CollectionId == collections[0].Id);
            if (coll0PostCount < 1000 || coll0RxCount < 1000)
                throw new InvalidOperationException($"Validation failed: Collection 0 must have >= 1000 posts and >= 1000 reactions, got {coll0PostCount} posts and {coll0RxCount} reactions.");

            var coll1PostCount = postCollections.Count(pc => pc.CollectionId == collections[1].Id);
            var coll1RxCount = collectionReactions.Count(r => r.CollectionId == collections[1].Id);
            if (coll1PostCount < 1000 || coll1RxCount < 1000)
                throw new InvalidOperationException($"Validation failed: Collection 1 must have >= 1000 posts and >= 1000 reactions, got {coll1PostCount} posts and {coll1RxCount} reactions.");
        }

        await db.Collections.AddRangeAsync(collections, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        const int batchSize = 5000;
        for (var idx = 0; idx < postCollections.Count; idx += batchSize)
        {
            var chunk = postCollections.Skip(idx).Take(batchSize).ToList();
            await db.PostCollections.AddRangeAsync(chunk, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        for (var idx = 0; idx < collectionReactions.Count; idx += batchSize)
        {
            var chunk = collectionReactions.Skip(idx).Take(batchSize).ToList();
            await db.Reactions.AddRangeAsync(chunk, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        var collCsv = ExportCollectionsCsv(collections);
        var postCollCsv = ExportPostCollectionsCsv(postCollections);

        return new SeedCollectionsResult(collections.Count, postCollections.Count, collCsv, postCollCsv);
    }

    private static string InferActivityRole(Profile profile)
    {
        if (profile.Username.StartsWith("user_", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(profile.Username.AsSpan(5), out var idx))
        {
            var total = SeedConfig.Users.Max;
            var lurkerCutoff  = (int)Math.Round(total * SeedConfig.UserRoleDistribution["lurker"],  MidpointRounding.AwayFromZero);
            var casualCutoff  = lurkerCutoff + (int)Math.Round(total * SeedConfig.UserRoleDistribution["casual"], MidpointRounding.AwayFromZero);

            if (idx <= lurkerCutoff) return "lurker";
            if (idx <= casualCutoff) return "casual";
            return "power";
        }
        return "casual";
    }

    private static string ExportCollectionsCsv(IEnumerable<Collection> collections)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var filePath = Path.Combine(outputRoot, "collections.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("collection_id,profile_id,title,description,cover_image_url,privacy,created_at,updated_at");
        foreach (var c in collections)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{c.Id},{c.ProfileId},{EscapeCsv(c.Title)},{EscapeCsv(c.Description)},{EscapeCsv(c.CoverImageUrl)},{c.PrivacyLevel},{c.CreatedAt:O},{c.UpdatedAt:O}"));
        }

        return filePath;
    }

    private static string ExportPostCollectionsCsv(IEnumerable<PostCollection> postCollections)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var filePath = Path.Combine(outputRoot, "post-collections.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("collection_id,post_id");
        foreach (var pc in postCollections)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{pc.CollectionId},{pc.PostId}"));
        }

        return filePath;
    }

    private static List<CatalogCollectionItem>? TryLoadRealCollectionsCatalog()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seed", "catalogs", "real-collections-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "seed", "catalogs", "real-collections-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Favi-BE", "Favi-BE.API", "seed", "catalogs", "real-collections-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Favi-BE.API", "seed", "catalogs", "real-collections-catalog.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "seed", "catalogs", "real-collections-catalog.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "seed", "catalogs", "real-collections-catalog.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Favi-BE.API", "seed", "catalogs", "real-collections-catalog.json"))
        };

        var catalogPath = candidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(catalogPath))
            return null;

        try
        {
            var json = File.ReadAllText(catalogPath);
            return JsonSerializer.Deserialize<List<CatalogCollectionItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
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

public sealed class CatalogCollectionItem
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("index")]
    public int Index { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cover_url")]
    public string CoverUrl { get; set; } = string.Empty;
}

public readonly record struct SeedCollectionsResult(int CreatedCollections, int CreatedPostCollections, string CollectionsExportPath, string PostCollectionsExportPath);

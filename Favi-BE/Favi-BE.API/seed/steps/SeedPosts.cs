using System.Globalization;
using System.Text;
using System.Text.Json;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedPostsStep
{
    private static readonly string[] DefaultImageTags =
    [
        "travel", "food", "sports", "city", "nature", "technology", "lifestyle", "art", "fitness", "music"
    ];

    public async Task<SeedPostsResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Profile> profiles,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (profiles.Count == 0)
            throw new InvalidOperationException("Step 3 requires profiles from Step 1.");

        var targetPostCount = seedContext.Random.Next(SeedConfig.Posts.Min, SeedConfig.Posts.Max + 1);
        var catalog = TryLoadRealPostsCatalog();
        var hasCatalog = catalog != null && catalog.Count > 0;
        var runImageSet = hasCatalog ? null : EnsureRunImageSet(seedContext);

        var posts = new List<Post>(targetPostCount);
        var medias = new List<PostMedia>(targetPostCount);

        for (var i = 0; i < targetPostCount; i++)
        {
            var profile = PickProfileByRoleWeight(profiles, seedContext);
            var createdAt = BuildCreatedAt(seedContext);
            var postId = Guid.NewGuid();

            string caption;
            bool isNsfw;
            string mediaUrl;
            string? location;

            if (hasCatalog)
            {
                var item = catalog![i % catalog.Count];
                caption = !string.IsNullOrWhiteSpace(item.Caption) ? item.Caption : $"Seed post #{i + 1}";
                isNsfw = item.IsNsfw;
                mediaUrl = !string.IsNullOrWhiteSpace(item.Url) ? item.Url : item.LocalPath;
                location = item.Category;
            }
            else
            {
                caption = $"Seed post #{i + 1}";
                isNsfw = false;
                mediaUrl = runImageSet![i % runImageSet.Count];
                location = null;
            }

            var post = new Post
            {
                Id = postId,
                ProfileId = profile.Id,
                Caption = caption,
                Privacy = BuildPrivacy(seedContext),
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsArchived = false,
                IsNSFW = isNsfw,
                LocationName = location
            };

            var media = new PostMedia
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                ProfileId = profile.Id,
                Url = mediaUrl,
                ThumbnailUrl = mediaUrl,
                Position = 0,
                PublicId = $"seed/post/{postId:N}",
                Width = 1080,
                Height = 1080,
                Format = "jpg",
                IsAvatar = false,
                IsPoster = false
            };

            posts.Add(post);
            medias.Add(media);
        }

        ValidatePostsAndMedia(posts, medias, targetPostCount);

        await db.Posts.AddRangeAsync(posts, cancellationToken);
        await db.PostMedias.AddRangeAsync(medias, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var postsCsv = ExportPostsCsv(posts);
        var mediasCsv = ExportPostMediasCsv(medias);

        return new SeedPostsResult(posts.Count, medias.Count, postsCsv, mediasCsv);
    }

    private static Profile PickProfileByRoleWeight(IReadOnlyList<Profile> profiles, SeedContext seedContext)
    {
        var totalWeight = 0d;
        var weighted = new (Profile profile, double weight)[profiles.Count];

        for (var i = 0; i < profiles.Count; i++)
        {
            var role = InferActivityRole(profiles[i]);
            var weight = role switch
            {
                "power" => 10d,
                "casual" => 3d,
                _ => 0.5d
            };

            totalWeight += weight;
            weighted[i] = (profiles[i], weight);
        }

        var roll = seedContext.Random.NextDouble() * totalWeight;
        foreach (var item in weighted)
        {
            roll -= item.weight;
            if (roll <= 0)
                return item.profile;
        }

        return weighted[^1].profile;
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

    private static PrivacyLevel BuildPrivacy(SeedContext seedContext)
    {
        var roll = seedContext.Random.NextDouble();
        if (roll < 0.80) return PrivacyLevel.Public;
        if (roll < 0.95) return PrivacyLevel.Followers;
        return PrivacyLevel.Private;
    }

    private static DateTime BuildCreatedAt(SeedContext seedContext)
    {
        var now = DateTime.UtcNow;
        return now
            .AddDays(-seedContext.Random.Next(0, 30))
            .AddHours(-seedContext.Random.Next(0, 24))
            .AddMinutes(-seedContext.Random.Next(0, 60));
    }

    private static void ValidatePostsAndMedia(
        IReadOnlyCollection<Post> posts,
        IReadOnlyCollection<PostMedia> medias,
        int expectedCount)
    {
        if (posts.Count != expectedCount)
            throw new InvalidOperationException("Validation failed: posts count mismatch.");

        if (medias.Count != expectedCount)
            throw new InvalidOperationException("Validation failed: post-medias count mismatch.");

        var postIds = posts.Select(p => p.Id).ToHashSet();
        if (medias.Any(m => m.PostId is null || !postIds.Contains(m.PostId.Value)))
            throw new InvalidOperationException("Validation failed: media has invalid PostId foreign key.");

        if (medias.Any(m => string.IsNullOrWhiteSpace(m.Url)))
            throw new InvalidOperationException("Validation failed: media URL is empty.");

        var mediaCoverage = medias
            .Where(m => m.PostId.HasValue)
            .GroupBy(m => m.PostId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        if (posts.Any(p => !mediaCoverage.TryGetValue(p.Id, out var cnt) || cnt != 1))
            throw new InvalidOperationException("Validation failed: each post must have exactly one media.");
    }

    private static List<string> EnsureRunImageSet(SeedContext seedContext)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var catalogPath = Path.Combine(outputRoot, SeedConfig.OutputPaths.ImageCatalogFileName);
        var runImageSetPath = Path.Combine(outputRoot, SeedConfig.OutputPaths.RunImageSetFileName);

        List<string> catalog;
        if (File.Exists(catalogPath))
        {
            catalog = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(catalogPath)) ?? [];
        }
        else
        {
            catalog = BuildImageCatalog(SeedConfig.ImageCatalogMinSize);
            File.WriteAllText(catalogPath, JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true }));
        }

        if (catalog.Count < SeedConfig.ImageCatalogMinSize)
            throw new InvalidOperationException("Validation failed: image catalog is smaller than configured minimum size.");

        List<string> runImageSet;
        if (File.Exists(runImageSetPath))
        {
            runImageSet = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(runImageSetPath)) ?? [];
        }
        else
        {
            var subsetSize = Math.Min(500, catalog.Count);
            runImageSet = catalog
                .OrderBy(url => StableSeed.FromString($"{seedContext.SeedKey}:{url}"))
                .Take(subsetSize)
                .ToList();

            File.WriteAllText(runImageSetPath, JsonSerializer.Serialize(runImageSet, new JsonSerializerOptions { WriteIndented = true }));
        }

        if (runImageSet.Count == 0)
            throw new InvalidOperationException("Validation failed: run image set is empty.");

        return runImageSet;
    }

    private static List<string> BuildImageCatalog(int size)
    {
        var results = new List<string>(size);
        var widths = new[] { 720, 800, 900, 1080 };
        var heights = new[] { 720, 800, 900, 1080 };

        for (var i = 0; i < size; i++)
        {
            var tag = DefaultImageTags[i % DefaultImageTags.Length];
            var width = widths[i % widths.Length];
            var height = heights[(i / widths.Length) % heights.Length];
            var lockId = i + 1;
            results.Add($"https://loremflickr.com/{width}/{height}/{tag}?lock={lockId}");
        }

        return results;
    }

    private static string ExportPostsCsv(IEnumerable<Post> posts)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var filePath = Path.Combine(outputRoot, "posts.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("post_id,profile_id,caption,privacy,created_at,updated_at,is_archived,is_nsfw,location");
        foreach (var post in posts)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{post.Id},{post.ProfileId},{EscapeCsv(post.Caption)},{post.Privacy},{post.CreatedAt:O},{post.UpdatedAt:O},{post.IsArchived},{post.IsNSFW},{EscapeCsv(post.LocationName)}"));
        }

        return filePath;
    }

    private static List<CatalogPostItem>? TryLoadRealPostsCatalog()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seed", "catalogs", "real-posts-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "seed", "catalogs", "real-posts-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Favi-BE", "Favi-BE.API", "seed", "catalogs", "real-posts-catalog.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "seed", "catalogs", "real-posts-catalog.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "seed", "catalogs", "real-posts-catalog.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Favi-BE.API", "seed", "catalogs", "real-posts-catalog.json"))
        };

        var catalogPath = candidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(catalogPath))
            return null;

        try
        {
            var json = File.ReadAllText(catalogPath);
            return JsonSerializer.Deserialize<List<CatalogPostItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static string ExportPostMediasCsv(IEnumerable<PostMedia> medias)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var filePath = Path.Combine(outputRoot, "post-medias.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("media_id,post_id,profile_id,url,thumbnail_url,position,public_id,width,height,format,is_avatar,is_poster");
        foreach (var media in medias)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{media.Id},{media.PostId},{media.ProfileId},{EscapeCsv(media.Url)},{EscapeCsv(media.ThumbnailUrl)},{media.Position},{media.PublicId},{media.Width},{media.Height},{media.Format},{media.IsAvatar},{media.IsPoster}"));
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

public sealed class CatalogPostItem
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("index")]
    public int Index { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("local_path")]
    public string? LocalPath { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("caption")]
    public string Caption { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("caption_en")]
    public string? CaptionEn { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [System.Text.Json.Serialization.JsonPropertyName("is_nsfw")]
    public bool IsNsfw { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("skin_ratio")]
    public double SkinRatio { get; set; }
}

public readonly record struct SeedPostsResult(int CreatedPosts, int CreatedPostMedias, string PostsExportPath, string PostMediasExportPath);


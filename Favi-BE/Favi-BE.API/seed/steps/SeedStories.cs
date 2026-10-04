using System.Globalization;
using System.Text;
using System.Text.Json;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedStoriesStep
{
    public async Task<SeedStoriesResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Profile> profiles,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (profiles.Count == 0)
            throw new InvalidOperationException("Step 7 requires profiles from Step 1.");

        var storyCatalog = TryLoadRealStoriesCatalog();
        var hasCatalog = storyCatalog != null && storyCatalog.Count > 0;
        var runImageSet = hasCatalog ? null : LoadRunImageSet();
        var now = DateTime.UtcNow;
        var stories = new List<Story>();
        var totalStoriesCreated = 0;
        var orderedProfiles = profiles.OrderBy(p => p.Username).ToList();

        // 1. Guarantee that the 1046 accounts (indices 105..1150) followed by user_00001 and user_00002 each have 1 active story
        var curatorFolloweeCount = Math.Min(1150, orderedProfiles.Count);
        for (var i = 105; i < curatorFolloweeCount; i++)
        {
            var profile = orderedProfiles[i];
            var storyId = Guid.NewGuid();
            string imageUrl;
            if (hasCatalog)
            {
                imageUrl = storyCatalog![totalStoriesCreated % storyCatalog.Count].Url;
            }
            else
            {
                imageUrl = $"https://loremflickr.com/1080/1920/nature?lock={totalStoriesCreated + 1}";
            }
            totalStoriesCreated++;
            var createdAt = now.AddMinutes(-seedContext.Random.Next(1, 1380)); // within 23h so still active

            stories.Add(new Story
            {
                Id = storyId,
                ProfileId = profile.Id,
                MediaUrl = imageUrl,
                ThumbnailUrl = imageUrl,
                MediaPublicId = $"seed/story/{storyId:N}",
                MediaWidth = 1080,
                MediaHeight = 1920,
                MediaFormat = "jpg",
                Privacy = BuildPrivacy(seedContext),
                IsArchived = false,
                IsNSFW = false,
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddHours(24)
            });
        }

        // 2. Add additional stories for power users if quota allows
        for (var i = 0; i < 105 && stories.Count < SeedConfig.Stories.Max; i++)
        {
            var profile = orderedProfiles[i];
            var storyId = Guid.NewGuid();
            string imageUrl;
            if (hasCatalog)
            {
                imageUrl = storyCatalog![totalStoriesCreated % storyCatalog.Count].Url;
            }
            else
            {
                imageUrl = $"https://loremflickr.com/1080/1920/nature?lock={totalStoriesCreated + 1}";
            }
            totalStoriesCreated++;
            var createdAt = now.AddMinutes(-seedContext.Random.Next(1, 1380));

            stories.Add(new Story
            {
                Id = storyId,
                ProfileId = profile.Id,
                MediaUrl = imageUrl,
                ThumbnailUrl = imageUrl,
                MediaPublicId = $"seed/story/{storyId:N}",
                MediaWidth = 1080,
                MediaHeight = 1920,
                MediaFormat = "jpg",
                Privacy = BuildPrivacy(seedContext),
                IsArchived = false,
                IsNSFW = false,
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddHours(24)
            });
        }

        ValidateStories(stories, profiles);

        await db.Stories.AddRangeAsync(stories, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var csvPath = ExportStoriesCsv(stories);
        return new SeedStoriesResult(stories.Count, csvPath);
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

    private static PrivacyLevel BuildPrivacy(SeedContext seedContext)
    {
        var roll = seedContext.Random.NextDouble();
        if (roll < 0.80) return PrivacyLevel.Public;
        if (roll < 0.95) return PrivacyLevel.Followers;
        return PrivacyLevel.Private;
    }

    private static void ValidateStories(List<Story> stories, IReadOnlyList<Profile> profiles)
    {
        if (stories.Count < SeedConfig.Stories.Min || stories.Count > SeedConfig.Stories.Max)
            throw new InvalidOperationException($"Seed validation failed: stories count {stories.Count} is outside expected range [{SeedConfig.Stories.Min}, {SeedConfig.Stories.Max}].");

        var profileIds = profiles.Select(p => p.Id).ToHashSet();
        if (stories.Any(s => !profileIds.Contains(s.ProfileId)))
            throw new InvalidOperationException("Seed validation failed: story has invalid ProfileId foreign key.");

        if (stories.Any(s => string.IsNullOrWhiteSpace(s.MediaUrl)))
            throw new InvalidOperationException("Seed validation failed: story media URL is empty.");

        if (stories.Any(s => s.ExpiresAt <= s.CreatedAt))
            throw new InvalidOperationException("Seed validation failed: story ExpiresAt must be after CreatedAt.");

        if (stories.Any(s => s.ExpiresAt <= DateTime.UtcNow))
            throw new InvalidOperationException("Seed validation failed: seeded stories must be active (ExpiresAt in future).");
    }

    private static List<string> LoadRunImageSet()
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        var runImageSetPath = Path.Combine(outputRoot, SeedConfig.OutputPaths.RunImageSetFileName);

        if (!File.Exists(runImageSetPath))
            throw new InvalidOperationException($"Run image set not found at '{runImageSetPath}'. Run Step 3 (SeedPosts) first to generate it.");

        var images = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(runImageSetPath)) ?? [];
        if (images.Count == 0)
            throw new InvalidOperationException("Seed validation failed: run image set is empty.");

        return images;
    }

    private static string ExportStoriesCsv(IEnumerable<Story> stories)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var filePath = Path.Combine(outputRoot, "stories.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("story_id,profile_id,media_url,thumbnail_url,privacy,created_at,expires_at,is_archived,is_nsfw");
        foreach (var s in stories)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{s.Id},{s.ProfileId},{s.MediaUrl},{s.ThumbnailUrl},{s.Privacy},{s.CreatedAt:O},{s.ExpiresAt:O},{s.IsArchived},{s.IsNSFW}"));
        }

        return filePath;
    }

    private static List<CatalogStoryItem>? TryLoadRealStoriesCatalog()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seed", "catalogs", "real-stories-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "seed", "catalogs", "real-stories-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Favi-BE", "Favi-BE.API", "seed", "catalogs", "real-stories-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Favi-BE.API", "seed", "catalogs", "real-stories-catalog.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "seed", "catalogs", "real-stories-catalog.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "seed", "catalogs", "real-stories-catalog.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Favi-BE.API", "seed", "catalogs", "real-stories-catalog.json"))
        };

        var catalogPath = candidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(catalogPath))
            return null;

        try
        {
            var json = File.ReadAllText(catalogPath);
            return JsonSerializer.Deserialize<List<CatalogStoryItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}

public sealed class CatalogStoryItem
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("index")]
    public int Index { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("caption")]
    public string? Caption { get; set; }
}

public readonly record struct SeedStoriesResult(int CreatedStories, string ExportPath);

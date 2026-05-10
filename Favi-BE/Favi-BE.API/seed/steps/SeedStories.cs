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

        var runImageSet = LoadRunImageSet();
        var now = DateTime.UtcNow;
        var stories = new List<Story>();

        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            var role = InferActivityRole(profile);

            var storyCount = role switch
            {
                "power"  => seedContext.Random.Next(2, 5),    // 2-4 stories
                "casual" => seedContext.Random.NextDouble() < 0.40 ? 1 : 0,
                _        => 0
            };

            for (var s = 0; s < storyCount; s++)
            {
                var storyId = Guid.NewGuid();
                var imageUrl = runImageSet[(i * 7 + s) % runImageSet.Count];
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
}

public readonly record struct SeedStoriesResult(int CreatedStories, string ExportPath);

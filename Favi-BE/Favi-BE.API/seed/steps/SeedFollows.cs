using System.Globalization;
using System.Text;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedFollowsStep
{
    private const double PreferentialAlpha = 1.05;

    public async Task<SeedFollowsResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Profile> profiles,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (profiles is null || profiles.Count < 2)
            throw new InvalidOperationException("Step 2 requires at least 2 profiles.");

        var orderedProfiles = profiles.OrderBy(p => p.Username).ToList();
        var profileIds = orderedProfiles.Select(p => p.Id).ToArray();

        var feasibleMax = (long)profileIds.Length * (profileIds.Length - 1);
        if (feasibleMax <= 0)
            throw new InvalidOperationException("Unable to generate follows with current profile set.");

        var boundedMin = (int)Math.Min((long)SeedConfig.Follows.Min, feasibleMax);
        var boundedMax = (int)Math.Min((long)SeedConfig.Follows.Max, feasibleMax);
        var targetFollowCount = seedContext.Random.Next(boundedMin, boundedMax + 1);

        var follows = GenerateFollows(seedContext, profileIds, targetFollowCount);

        ValidateGeneratedGraph(follows, profileIds, boundedMin, boundedMax);

        const int batchSize = 25000;
        for (var i = 0; i < follows.Count; i += batchSize)
        {
            var chunk = follows.Skip(i).Take(batchSize).ToList();
            await db.Follows.AddRangeAsync(chunk, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        var exportPath = ExportFollowsCsv(follows);

        return new SeedFollowsResult(follows.Count, exportPath);
    }

    private static List<Follow> GenerateFollows(SeedContext seedContext, Guid[] profileIds, int targetFollowCount)
    {
        var follows = new List<Follow>(targetFollowCount);
        var existingEdges = new HashSet<(Guid FollowerId, Guid FolloweeId)>();

        // 1. Ensure at least 105 celebrities have >= 1,020 followers
        var celebrityCount = Math.Min(105, profileIds.Length);
        var celebrities = profileIds.Take(celebrityCount).ToArray();
        var targetFollowersPerCeleb = Math.Min(1020, profileIds.Length - 1);

        foreach (var celeb in celebrities)
        {
            var otherProfiles = profileIds.Where(p => p != celeb).ToList();
            // Shuffle
            for (var i = otherProfiles.Count - 1; i > 0; i--)
            {
                var j = seedContext.Random.Next(i + 1);
                (otherProfiles[i], otherProfiles[j]) = (otherProfiles[j], otherProfiles[i]);
            }

            foreach (var follower in otherProfiles.Take(targetFollowersPerCeleb))
            {
                if (existingEdges.Add((follower, celeb)))
                {
                    follows.Add(new Follow
                    {
                        FollowerId = follower,
                        FolloweeId = celeb,
                        CreatedAt = BuildCreatedAt(seedContext)
                    });
                }
            }
        }

        // 2. Ensure at least 105 curators follow >= 1,020 accounts (user_00001 and user_00002 are both included)
        var curators = profileIds.Take(celebrityCount).ToArray();
        var targetFolloweesPerCurator = Math.Min(1020, profileIds.Length - 1);

        foreach (var curator in curators)
        {
            List<Guid> followeeCandidates;
            if (curator == profileIds[0] || curator == profileIds[1])
            {
                // Both user_00001 and user_00002 follow the accounts 105..1150 (1,046 accounts that have active stories)
                followeeCandidates = profileIds.Skip(105).Take(1046).Where(p => p != curator).ToList();
            }
            else
            {
                var otherProfiles = profileIds.Where(p => p != curator).ToList();
                for (var i = otherProfiles.Count - 1; i > 0; i--)
                {
                    var j = seedContext.Random.Next(i + 1);
                    (otherProfiles[i], otherProfiles[j]) = (otherProfiles[j], otherProfiles[i]);
                }
                followeeCandidates = otherProfiles;
            }

            var limit = (curator == profileIds[0] || curator == profileIds[1])
                ? Math.Min(1046, followeeCandidates.Count)
                : targetFolloweesPerCurator;

            foreach (var followee in followeeCandidates.Take(limit))
            {
                if (existingEdges.Add((curator, followee)))
                {
                    follows.Add(new Follow
                    {
                        FollowerId = curator,
                        FolloweeId = followee,
                        CreatedAt = BuildCreatedAt(seedContext)
                    });
                }
            }
        }

        // 3. Fill remaining quota up to targetFollowCount if needed
        var attempts = 0;
        var maxAttempts = targetFollowCount * 2;
        var roundRobinIndex = 0;

        while (follows.Count < targetFollowCount && attempts < maxAttempts)
        {
            attempts++;
            var follower = profileIds[roundRobinIndex % profileIds.Length];
            roundRobinIndex++;
            var followee = profileIds[seedContext.Random.Next(profileIds.Length)];

            if (follower == followee)
                continue;

            if (existingEdges.Add((follower, followee)))
            {
                follows.Add(new Follow
                {
                    FollowerId = follower,
                    FolloweeId = followee,
                    CreatedAt = BuildCreatedAt(seedContext)
                });
            }
        }

        if (follows.Count == 0)
            throw new InvalidOperationException("Generated social graph is empty.");

        return follows;
    }


    private static DateTime BuildCreatedAt(SeedContext seedContext)
    {
        var now = DateTime.UtcNow;
        return now
            .AddDays(-seedContext.Random.Next(0, 30))
            .AddHours(-seedContext.Random.Next(0, 24))
            .AddMinutes(-seedContext.Random.Next(0, 60));
    }

    private static void ValidateGeneratedGraph(
        List<Follow> follows,
        IReadOnlyCollection<Guid> profileIds,
        int boundedMin,
        int boundedMax)
    {
        if (follows.Count == 0)
            throw new InvalidOperationException("Validation failed: graph is empty.");

        if (follows.Any(f => f.FollowerId == f.FolloweeId))
            throw new InvalidOperationException("Validation failed: self-follow detected.");

        var uniqueEdges = follows
            .Select(f => (f.FollowerId, f.FolloweeId))
            .Distinct()
            .Count();

        if (uniqueEdges != follows.Count)
            throw new InvalidOperationException("Validation failed: duplicate follow edge detected.");

        if (follows.Count < boundedMin || follows.Count > boundedMax)
            throw new InvalidOperationException("Validation failed: follows count is out of expected range.");

        var profileSet = profileIds.ToHashSet();
        if (follows.Any(f => !profileSet.Contains(f.FollowerId) || !profileSet.Contains(f.FolloweeId)))
            throw new InvalidOperationException("Validation failed: invalid follow foreign key detected.");

        if (follows.GroupBy(f => f.FollowerId).Any(g => g.Count() >= profileIds.Count))
            throw new InvalidOperationException("Validation failed: a user exceeds max followees limit.");
    }

    private static string ExportFollowsCsv(IEnumerable<Follow> follows)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var filePath = Path.Combine(outputRoot, "follows.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("follower_id,followee_id,created_at");
        foreach (var follow in follows)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{follow.FollowerId},{follow.FolloweeId},{follow.CreatedAt:O}"));
        }

        return filePath;
    }
}

public readonly record struct SeedFollowsResult(int CreatedCount, string ExportPath);

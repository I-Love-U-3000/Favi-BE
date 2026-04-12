using System.Globalization;
using System.Text;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedFollowsStep
{
    private const int MinFolloweesPerUser = 0;
    private const int MaxFolloweesPerUser = 20;
    private const double PreferentialAlpha = 1.05;

    public async Task<SeedFollowsResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Profile> profiles,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (profiles is null || profiles.Count < 2)
            throw new InvalidOperationException("Step 2 requires at least 2 profiles.");

        var orderedProfiles = profiles.OrderBy(p => p.Id).ToList();
        var profileIds = orderedProfiles.Select(p => p.Id).ToArray();

        var feasibleMax = Math.Min(
            profileIds.Length * (profileIds.Length - 1),
            profileIds.Length * MaxFolloweesPerUser);

        if (feasibleMax <= 0)
            throw new InvalidOperationException("Unable to generate follows with current profile set.");

        var boundedMin = Math.Min(SeedConfig.Follows.Min, feasibleMax);
        var boundedMax = Math.Min(SeedConfig.Follows.Max, feasibleMax);
        var targetFollowCount = seedContext.Random.Next(boundedMin, boundedMax + 1);

        var follows = GenerateFollows(seedContext, profileIds, targetFollowCount);

        ValidateGeneratedGraph(follows, profileIds, boundedMin, boundedMax);

        await db.Follows.AddRangeAsync(follows, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var exportPath = ExportFollowsCsv(follows);

        return new SeedFollowsResult(follows.Count, exportPath);
    }

    private static List<Follow> GenerateFollows(SeedContext seedContext, Guid[] profileIds, int targetFollowCount)
    {
        var follows = new List<Follow>(targetFollowCount);
        var outDegree = profileIds.ToDictionary(id => id, _ => 0);
        var inDegree = profileIds.ToDictionary(id => id, _ => 0);
        var existingEdges = new HashSet<(Guid FollowerId, Guid FolloweeId)>();

        var celebrityBucketSize = Math.Max(1, (int)Math.Ceiling(profileIds.Length * 0.02));
        var celebritySet = profileIds
            .OrderBy(id => StableSeed.FromString($"{seedContext.SeedKey}:celeb:{id}"))
            .Take(celebrityBucketSize)
            .ToHashSet();

        var attempts = 0;
        var maxAttempts = Math.Max(20_000, targetFollowCount * 40);
        var roundRobinIndex = 0;

        while (follows.Count < targetFollowCount && attempts < maxAttempts)
        {
            var followerId = profileIds[roundRobinIndex % profileIds.Length];
            roundRobinIndex++;
            attempts++;

            if (outDegree[followerId] >= MaxFolloweesPerUser)
                continue;

            var followeeId = PickFollowee(seedContext, profileIds, followerId, outDegree, inDegree, existingEdges, celebritySet);
            if (followeeId is null)
                continue;

            var edge = (followerId, followeeId.Value);
            if (!existingEdges.Add(edge))
                continue;

            outDegree[followerId]++;
            inDegree[followeeId.Value]++;

            follows.Add(new Follow
            {
                FollowerId = followerId,
                FolloweeId = followeeId.Value,
                CreatedAt = BuildCreatedAt(seedContext)
            });
        }

        if (follows.Count == 0)
            throw new InvalidOperationException("Generated social graph is empty.");

        return follows;
    }

    private static Guid? PickFollowee(
        SeedContext seedContext,
        Guid[] profileIds,
        Guid followerId,
        Dictionary<Guid, int> outDegree,
        Dictionary<Guid, int> inDegree,
        HashSet<(Guid FollowerId, Guid FolloweeId)> existingEdges,
        HashSet<Guid> celebritySet)
    {
        var candidates = new List<(Guid Id, double Weight)>(profileIds.Length - 1);

        foreach (var candidateId in profileIds)
        {
            if (candidateId == followerId)
                continue;

            if (existingEdges.Contains((followerId, candidateId)))
                continue;

            var baseWeight = 1d + Math.Pow(inDegree[candidateId] + 1, PreferentialAlpha);
            if (celebritySet.Contains(candidateId))
                baseWeight *= 2.5d;

            if (outDegree[followerId] <= 3)
                baseWeight *= 1.1d;

            candidates.Add((candidateId, baseWeight));
        }

        if (candidates.Count == 0)
            return null;

        var totalWeight = candidates.Sum(c => c.Weight);
        var roll = seedContext.Random.NextDouble() * totalWeight;

        foreach (var candidate in candidates)
        {
            roll -= candidate.Weight;
            if (roll <= 0)
                return candidate.Id;
        }

        return candidates[^1].Id;
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

        if (follows.GroupBy(f => f.FollowerId).Any(g => g.Count() > MaxFolloweesPerUser))
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

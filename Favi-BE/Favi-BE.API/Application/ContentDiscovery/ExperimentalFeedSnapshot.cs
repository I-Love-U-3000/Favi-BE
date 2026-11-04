using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Favi_BE.API.Application.ContentDiscovery;

public sealed class ExperimentalFeedOptions
{
    public bool Enabled { get; set; }
    public string ArtifactPath { get; set; } = "";
    public string ArtifactSha256 { get; set; } = "";
    public int MaxArtifactAgeHours { get; set; } = 24;
}

public sealed record RankedExperimentPost(Guid PostId, double Score);
public sealed record ExperimentUser(Guid UserId, RankedExperimentPost[] Ease, RankedExperimentPost[] Ppr);
public sealed record ExperimentArtifact(int SchemaVersion, string Version, DateTimeOffset CreatedAtUtc,
    int TrainingCutoffTick, string DatasetEventsSha256, string SelectionSha256, string SourceMappingSha256,
    ExperimentUser[] Users);

public sealed class ExperimentalFeedSnapshot
{
    private readonly Dictionary<Guid, (IReadOnlyList<Guid> Ease, IReadOnlyList<Guid> Ppr)> _users = [];
    private readonly ExperimentalFeedOptions _options;
    public bool Enabled => _options.Enabled;
    public string Version { get; } = "disabled";
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsStale(DateTimeOffset now) => now - CreatedAtUtc > TimeSpan.FromHours(_options.MaxArtifactAgeHours);

    public ExperimentalFeedSnapshot(IOptions<ExperimentalFeedOptions> options, TimeProvider time)
    {
        _options = options.Value;
        if (!Enabled) return;
        if (!Path.IsPathFullyQualified(_options.ArtifactPath) || !Digest(_options.ArtifactSha256) ||
            _options.MaxArtifactAgeHours is < 1 or > 168)
            throw new InvalidDataException("Invalid enabled ExperimentalFeed configuration");
        const int maxBytes = 16 * 1024 * 1024;
        if (new FileInfo(_options.ArtifactPath).Length > maxBytes)
            throw new InvalidDataException("Experimental artifact exceeds 16 MiB");
        var bytes = File.ReadAllBytes(_options.ArtifactPath);
        if (bytes.Length > maxBytes || !Convert.ToHexString(SHA256.HashData(bytes)).Equals(
                _options.ArtifactSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Experimental artifact checksum mismatch");
        var artifact = JsonSerializer.Deserialize<ExperimentArtifact>(bytes, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 16
        }) ?? throw new InvalidDataException("Missing experimental artifact");
        if (artifact.SchemaVersion != 1 || string.IsNullOrWhiteSpace(artifact.Version) || artifact.Version.Length > 128 ||
            artifact.TrainingCutoffTick <= 0 || !Digest(artifact.DatasetEventsSha256) ||
            !Digest(artifact.SelectionSha256) || !Digest(artifact.SourceMappingSha256) ||
            artifact.CreatedAtUtc.Offset != TimeSpan.Zero || artifact.CreatedAtUtc.Year < 2020 ||
            artifact.CreatedAtUtc > time.GetUtcNow().AddMinutes(5) || artifact.Users is null || artifact.Users.Length > 1000)
            throw new InvalidDataException("Invalid experimental artifact metadata");
        Version = artifact.Version;
        CreatedAtUtc = artifact.CreatedAtUtc;
        foreach (var user in artifact.Users)
        {
            if (user is null || user.UserId == Guid.Empty || _users.ContainsKey(user.UserId))
                throw new InvalidDataException("Duplicate/empty experimental viewer ID");
            _users.Add(user.UserId, (Validate(user.Ease), Validate(user.Ppr)));
        }
    }

    public IReadOnlyList<Guid>? Get(Guid userId, string method)
        => !_users.TryGetValue(userId, out var row) ? null : method switch
        {
            "ease" => row.Ease, "ppr" => row.Ppr,
            _ => throw new ArgumentException("Unsupported experimental method", nameof(method))
        };

    private static IReadOnlyList<Guid> Validate(RankedExperimentPost[]? posts)
    {
        if (posts is null || posts.Length > 300) throw new InvalidDataException("Invalid experimental candidate bound");
        var seen = new HashSet<Guid>();
        double previous = double.PositiveInfinity;
        foreach (var post in posts)
        {
            if (post is null || post.PostId == Guid.Empty || !seen.Add(post.PostId) || !double.IsFinite(post.Score) || post.Score > previous)
                throw new InvalidDataException("Invalid experimental candidate identity/score/order");
            previous = post.Score;
        }
        return Array.AsReadOnly(posts.Select(p => p.PostId).ToArray());
    }

    private static bool Digest(string? value) => value is not null && Regex.IsMatch(value, "\\A[0-9a-fA-F]{64}\\z");
}

internal sealed class ExperimentalFeedStartupValidator(ExperimentalFeedSnapshot snapshot) : IHostedService
{
    public Task StartAsync(CancellationToken ct) { _ = snapshot.Enabled; return Task.CompletedTask; }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

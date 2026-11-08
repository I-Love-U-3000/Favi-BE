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
public sealed record ExperimentUser(Guid UserId, RankedExperimentPost[]? Ease = null, RankedExperimentPost[]? Ppr = null,
    Dictionary<string, RankedExperimentPost[]>? Ranks = null);
public sealed record ExperimentMethod(string Family, int Threshold, Dictionary<string, JsonElement> Parameters,
    int? Epoch, int? OptimizerSeed);
public sealed record ExperimentArtifact(int SchemaVersion, string Version, DateTimeOffset CreatedAtUtc,
    int TrainingCutoffTick, string DatasetEventsSha256, string SelectionSha256, string SourceMappingSha256,
    ExperimentUser[] Users, Dictionary<string, ExperimentMethod>? Methods = null);

public sealed class ExperimentalFeedSnapshot
{
    private readonly Dictionary<Guid, (IReadOnlyList<Guid> Ease, IReadOnlyList<Guid> Ppr)> _users = [];
    private readonly ExperimentalFeedOptions _options;
    private readonly Dictionary<Guid, Dictionary<string, IReadOnlyList<Guid>>> _adaptive = [];
    private readonly Dictionary<string, int> _thresholds = [];
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
        if (artifact.SchemaVersion is not (1 or 2) || string.IsNullOrWhiteSpace(artifact.Version) || artifact.Version.Length > 128 ||
            artifact.TrainingCutoffTick <= 0 || !Digest(artifact.DatasetEventsSha256) ||
            !Digest(artifact.SelectionSha256) || !Digest(artifact.SourceMappingSha256) ||
            artifact.CreatedAtUtc.Offset != TimeSpan.Zero || artifact.CreatedAtUtc.Year < 2020 ||
            artifact.CreatedAtUtc > time.GetUtcNow().AddMinutes(5) || artifact.Users is null || artifact.Users.Length > 1000)
            throw new InvalidDataException("Invalid experimental artifact metadata");
        Version = artifact.Version;
        CreatedAtUtc = artifact.CreatedAtUtc;
        if (artifact.SchemaVersion == 1)
        {
            if (artifact.Methods is not null) throw new InvalidDataException("Schema1 cannot declare adaptive methods");
            _thresholds.Add("ease", 1);
            _thresholds.Add("ppr", 1);
        }
        else
        {
            if (artifact.Methods is null || artifact.Methods.Count != 2 ||
                !artifact.Methods.ContainsKey("B1") || !artifact.Methods.ContainsKey("R"))
                throw new InvalidDataException("Schema2 requires exactly B1/R");
            foreach (var (label, method) in artifact.Methods)
            {
                ValidateMethod(label, method);
                _thresholds.Add(label, method.Threshold);
            }
        }
        foreach (var user in artifact.Users)
        {
            if (user is null || user.UserId == Guid.Empty || _users.ContainsKey(user.UserId) || _adaptive.ContainsKey(user.UserId))
                throw new InvalidDataException("Duplicate/empty experimental viewer ID");
            if (artifact.SchemaVersion == 1)
            {
                if (user.Ranks is not null) throw new InvalidDataException("Schema1 cannot contain adaptive ranks");
                _users.Add(user.UserId, (Validate(user.Ease), Validate(user.Ppr)));
            }
            else
            {
                if (user.Ease is not null || user.Ppr is not null || user.Ranks is null || user.Ranks.Count != 2 ||
                    !user.Ranks.ContainsKey("B1") || !user.Ranks.ContainsKey("R"))
                    throw new InvalidDataException("Schema2 requires exactly B1/R ranks");
                _adaptive.Add(user.UserId, user.Ranks.ToDictionary(p => p.Key, p => Validate(p.Value)));
            }
        }
    }

    public IReadOnlyList<Guid>? Get(Guid userId, string method)
    {
        _ = GetThreshold(method);
        if (method is "B1" or "R") return _adaptive.TryGetValue(userId, out var adaptive) ? adaptive[method] : null;
        return !_users.TryGetValue(userId, out var row) ? null : method switch
        {
            "ease" => row.Ease, "ppr" => row.Ppr,
            _ => throw new ArgumentException("Unsupported experimental method", nameof(method))
        };
    }

    // An unavailable method falls back explicitly rather than aliasing a different model/schema.
    public int GetThreshold(string method) => _thresholds.TryGetValue(method, out var threshold) ? threshold :
        method is "ease" or "ppr" or "B1" or "R" ? 1 : throw new ArgumentException("Unsupported experimental method", nameof(method));

    private static void ValidateMethod(string label, ExperimentMethod? method)
    {
        if (method is null || method.Threshold is not (1 or 5) || (label == "B1" && method.Threshold != 1) ||
            method.Parameters is null || method.Family is not ("ease" or "ppr" or "lightgcn"))
            throw new InvalidDataException("Invalid adaptive method metadata");
        var expected = method.Family switch
        {
            "ease" or "ppr" => new[] { "value" },
            _ => new[] { "dimensions", "layers", "learning_rate", "regularization", "epochs", "check_every" }
        };
        if (!method.Parameters.Keys.Order().SequenceEqual(expected.Order()) ||
            method.Parameters.Values.Any(v => v.ValueKind != JsonValueKind.Number || !v.TryGetDouble(out var n) || !double.IsFinite(n) || n <= 0))
            throw new InvalidDataException("Invalid adaptive model parameters");
        if (method.Family == "lightgcn")
        {
            if (method.Epoch is null or < 1 || method.OptimizerSeed != 20261103 ||
                !method.Parameters["epochs"].TryGetInt32(out var epochs) || epochs != method.Epoch ||
                method.Epoch > 300 || method.Epoch % 10 != 0 ||
                !method.Parameters["layers"].TryGetInt32(out var layers) || layers is < 1 or > 2 ||
                method.Parameters["dimensions"].GetDouble() != 64 || method.Parameters["check_every"].GetDouble() != 10 ||
                method.Parameters["regularization"].GetDouble() != 0.0001 ||
                method.Parameters["learning_rate"].GetDouble() is not (0.001 or 0.01))
                throw new InvalidDataException("Invalid adaptive LightGCN metadata");
        }
        else if (method.Epoch != 0 || method.OptimizerSeed is not null ||
                 (method.Family == "ppr" && method.Parameters["value"].GetDouble() is not (0.5 or 0.85 or 0.95)) ||
                 (method.Family == "ease" && method.Parameters["value"].GetDouble() is not (1 or 10 or 100 or 1000)))
            throw new InvalidDataException("Invalid adaptive solver metadata");
    }

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

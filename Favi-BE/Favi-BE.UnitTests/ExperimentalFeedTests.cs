using System.Security.Cryptography;
using System.Text.Json;
using Favi_BE.API.Application.ContentDiscovery;
using Microsoft.Extensions.Options;

namespace Favi_BE.UnitTests;

public sealed class ExperimentalFeedTests
{
    [Fact]
    public void DisabledSnapshotDoesNotReadConfiguredMissingFile()
    {
        var snapshot = new ExperimentalFeedSnapshot(Options.Create(new ExperimentalFeedOptions
        { Enabled = false, ArtifactPath = "missing-file" }), TimeProvider.System);
        Assert.False(snapshot.Enabled);
    }

    [Fact]
    public void ValidSnapshotPreservesRankAndHasExplicitStaleness()
    {
        var user = Guid.NewGuid();
        var posts = new[] { new RankedExperimentPost(Guid.NewGuid(), 2), new RankedExperimentPost(Guid.NewGuid(), -1) };
        WithArtifact(Artifact([new(user, posts, posts)]), options =>
        {
            var snapshot = new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System);
            Assert.Equal(posts.Select(p => p.PostId), snapshot.Get(user, "ease"));
            Assert.Null(snapshot.Get(Guid.NewGuid(), "ppr"));
            Assert.True(snapshot.IsStale(DateTimeOffset.UtcNow.AddHours(25)));
            Assert.False(snapshot.IsStale(DateTimeOffset.UtcNow));
        });
    }

    [Fact]
    public void WrongChecksumIsRejectedBeforeServing()
    {
        WithArtifact(Artifact([]), options =>
        {
            options.ArtifactSha256 = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System));
        });
    }

    [Fact]
    public void DuplicateCandidateAndAscendingScoreAreRejected()
    {
        var first = new RankedExperimentPost(Guid.NewGuid(), 2);
        foreach (var posts in new[] { new[] { first, first }, new[] { first, new RankedExperimentPost(Guid.NewGuid(), 3) } })
            WithArtifact(Artifact([new(Guid.NewGuid(), posts, posts)]), options =>
                Assert.Throws<InvalidDataException>(() => new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System)));
    }

    [Fact]
    public void OversizedCandidatePoolAndFutureTimestampAreRejected()
    {
        var posts = Enumerable.Range(0, 301).Select(i => new RankedExperimentPost(Guid.NewGuid(), -i)).ToArray();
        foreach (var artifact in new[] { Artifact([new(Guid.NewGuid(), posts, [])]), Artifact([]) with { CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(1) } })
            WithArtifact(artifact, options => Assert.Throws<InvalidDataException>(() =>
                new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System)));
    }

    [Fact]
    public void AdaptiveSnapshotKeepsMethodsThresholdsAndUnknownHistorySeparate()
    {
        var user = Guid.NewGuid();
        var post = new RankedExperimentPost(Guid.NewGuid(), 2);
        var parameters = new Dictionary<string, JsonElement> { ["value"] = JsonSerializer.SerializeToElement(1000) };
        var artifact = Artifact([new(user, Ranks: new() { ["B1"] = [post], ["R"] = [] })]) with
        {
            SchemaVersion = 2,
            Methods = new() { ["B1"] = new("ease", 1, parameters, 0, null), ["R"] = new("ease", 5, parameters, 0, null) }
        };
        WithArtifact(artifact, options =>
        {
            var snapshot = new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System);
            Assert.Equal(5, snapshot.GetThreshold("R"));
            Assert.Equal(1, snapshot.GetThreshold("B1"));
            Assert.Equal(new[] { post.PostId }, snapshot.Get(user, "B1"));
            Assert.Empty(snapshot.Get(user, "R")!);
            Assert.Null(snapshot.Get(user, "ease"));
        });
        foreach (var broken in new[]
        {
            artifact with { Methods = new() { ["B1"] = artifact.Methods["B1"] } },
            artifact with { Methods = new() { ["B1"] = artifact.Methods["B1"] with { Threshold = 5 }, ["R"] = artifact.Methods["R"] } },
            artifact with { Users = [new(user, Ranks: new() { ["B1"] = [post], ["R"] = [post, post] })] }
        })
            WithArtifact(broken, options => Assert.Throws<InvalidDataException>(() =>
                new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System)));
    }

    private static ExperimentArtifact Artifact(ExperimentUser[] users) => new(1, "test-version", DateTimeOffset.UtcNow,
        24, new string('a',64), new string('b',64), new string('c',64), users);

    [Theory]
    [InlineData("ppr", 0)]
    [InlineData("lightgcn", 60)]
    public void AdaptiveMetadataSupportsFrozenFamiliesAndRejectsInvalidEpoch(string family, int epoch)
    {
        var parameters = family == "ppr" ? new Dictionary<string, double> { ["value"] = 0.85 } :
            new() { ["dimensions"] = 64, ["layers"] = 2, ["learning_rate"] = 0.01,
                ["regularization"] = 0.0001, ["epochs"] = epoch, ["check_every"] = 10 };
        var method = new ExperimentMethod(family, 1,
            parameters.ToDictionary(p => p.Key, p => JsonSerializer.SerializeToElement(p.Value)), epoch,
            family == "lightgcn" ? 20261103 : null);
        var artifact = Artifact([]) with { SchemaVersion = 2, Methods = new() { ["B1"] = method, ["R"] = method with { Threshold = 5 } } };
        WithArtifact(artifact, options => Assert.True(new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System).Enabled));
        artifact.Methods["R"] = method with { Threshold = 5, Epoch = epoch + 1 };
        WithArtifact(artifact, options => Assert.Throws<InvalidDataException>(() =>
            new ExperimentalFeedSnapshot(Options.Create(options), TimeProvider.System)));
    }

    private static void WithArtifact(ExperimentArtifact artifact, Action<ExperimentalFeedOptions> test)
    {
        var path = Path.Combine(Path.GetTempPath(), "favi-experiment-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(artifact, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            File.WriteAllBytes(path, bytes);
            test(new ExperimentalFeedOptions { Enabled = true, ArtifactPath = path,
                ArtifactSha256 = Convert.ToHexString(SHA256.HashData(bytes)) });
        }
        finally { File.Delete(path); }
    }
}

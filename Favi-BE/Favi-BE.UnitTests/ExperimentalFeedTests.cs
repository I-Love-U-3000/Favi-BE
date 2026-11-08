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

    private static ExperimentArtifact Artifact(ExperimentUser[] users) => new(1, "test-version", DateTimeOffset.UtcNow,
        24, new string('a',64), new string('b',64), new string('c',64), users);

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

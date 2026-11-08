using Favi_BE.API.DependencyInjection;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Auth.Application.Commands.SetProfileInterests;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileInterests;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.UnitTests;

public sealed class ColdStartReaderTests
{
    [Fact]
    public async Task PreferenceUpdatesIncrementVersionExactlyOnceThroughRealStore()
    {
        using var provider = Provider();
        var db = provider.GetRequiredService<AppDbContext>();
        var viewer = new Profile { Id = Guid.NewGuid(), Username = "version_test" };
        db.Profiles.Add(viewer);
        await db.SaveChangesAsync();
        var mediator = provider.GetRequiredService<IMediator>();
        Assert.Equal(1, (await mediator.Send(new SetProfileInterestsCommand(viewer.Id, ["food"], 0))).Data!.Version);
        Assert.Equal(2, (await mediator.Send(new SetProfileInterestsCommand(viewer.Id, ["travel"], 1))).Data!.Version);
        Assert.Equal(2, (await mediator.Send(new GetProfileInterestsQuery(viewer.Id)))!.Version);
    }

    [Fact]
    public async Task CatalogFiltersUnsafePrivateBannedAndDeletedAndHonorsTopics()
    {
        using var provider = Provider();
        var db = provider.GetRequiredService<AppDbContext>();
        var viewer = new Profile { Id = Guid.NewGuid(), Username = "viewer" };
        var author = new Profile { Id = Guid.NewGuid(), Username = "author" };
        var banned = new Profile { Id = Guid.NewGuid(), Username = "banned", IsBanned = true };
        db.Profiles.AddRange(viewer, author, banned);
        var food = Post(author, "food");
        var travel = Post(author, "travel");
        var nsfw = Post(author, "food"); nsfw.IsNSFW = true;
        var hidden = Post(author, "food"); hidden.Privacy = PrivacyLevel.Private;
        var archived = Post(author, "food"); archived.IsArchived = true;
        var deleted = Post(author, "food"); deleted.DeletedDayExpiredAt = DateTime.UtcNow;
        db.Posts.AddRange(food, travel, nsfw, hidden, archived, deleted, Post(banned, "food"));
        await db.SaveChangesAsync();
        var mediator = provider.GetRequiredService<IMediator>();
        var set = await mediator.Send(new SetProfileInterestsCommand(viewer.Id, ["food"], 0));
        Assert.Null(set.Error);
        var reader = provider.GetRequiredService<IColdStartQueryReader>();
        var ids = await reader.GetRankedPostIdsAsync(viewer.Id, default);
        Assert.NotNull(ids);
        Assert.Equal(food.Id, ids![0]);
        Assert.Equal(2, ids.Count);
        var loaded = await reader.LoadVisiblePostsAsync(viewer.Id, ids, default);
        Assert.Equal(2, loaded.Count);
        Assert.All(loaded, p => Assert.False(p.IsNSFW));
        food.Privacy = PrivacyLevel.Private;
        await db.SaveChangesAsync();
        loaded = await reader.LoadVisiblePostsAsync(viewer.Id, ids, default);
        Assert.Single(loaded);
        Assert.Equal(travel.Id, loaded[0].Id);
    }

    [Fact]
    public async Task PositiveHistoryUsesWarmPathAndNewMissingUserDoesNotLeakFeed()
    {
        using var provider = Provider();
        var db = provider.GetRequiredService<AppDbContext>();
        var viewer = new Profile { Id = Guid.NewGuid(), Username = "viewer" };
        var author = new Profile { Id = Guid.NewGuid(), Username = "author" };
        var post = Post(author, "food");
        db.Profiles.AddRange(viewer, author);
        db.Posts.Add(post);
        db.Reactions.Add(new Reaction { Id = Guid.NewGuid(), ProfileId = viewer.Id,
            PostId = post.Id, Type = ReactionType.Like, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var reader = provider.GetRequiredService<IColdStartQueryReader>();
        Assert.Null(await reader.GetRankedPostIdsAsync(viewer.Id, default));
        Assert.Empty((await reader.GetRankedPostIdsAsync(Guid.NewGuid(), default))!);
    }

    [Fact]
    public async Task GraphIdsAreRevalidatedAndInvalidScoresAreIgnored()
    {
        var graph = new FakeGraph();
        using var provider = Provider(graph);
        var db = provider.GetRequiredService<AppDbContext>();
        var viewer = new Profile { Id = Guid.NewGuid(), Username = "viewer" };
        var author = new Profile { Id = Guid.NewGuid(), Username = "author" };
        var good = Post(author, "food");
        var bad = Post(author, "food"); bad.IsNSFW = true;
        db.Profiles.AddRange(viewer, author);
        db.Posts.AddRange(good, bad);
        await db.SaveChangesAsync();
        graph.Candidates = [new(bad.Id, 100), new(good.Id, 50), new(Guid.NewGuid(), double.NaN)];
        var ids = await provider.GetRequiredService<IColdStartQueryReader>().GetRankedPostIdsAsync(viewer.Id, default);
        Assert.Equal(new[] { good.Id }, ids);
        Assert.Equal(300, graph.Limit);
    }

    [Fact]
    public async Task GraphHttpFailureFallsBackToCatalog()
    {
        using var provider = Provider(new FakeGraph { Fail = true });
        var db = provider.GetRequiredService<AppDbContext>();
        var viewer = new Profile { Id = Guid.NewGuid(), Username = "viewer" };
        var author = new Profile { Id = Guid.NewGuid(), Username = "author" };
        var post = Post(author, "food");
        db.Profiles.AddRange(viewer, author);
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        Assert.Equal(new[] { post.Id }, await provider.GetRequiredService<IColdStartQueryReader>()
            .GetRankedPostIdsAsync(viewer.Id, default));
    }

    [Fact]
    public async Task AdaptiveThresholdUsesDistinctCurrentPositivesAndStillChecksViewer()
    {
        using var provider = Provider();
        var db = provider.GetRequiredService<AppDbContext>();
        var viewer = new Profile { Id = Guid.NewGuid(), Username = "adaptive_viewer" };
        var author = new Profile { Id = Guid.NewGuid(), Username = "adaptive_author" };
        db.Profiles.AddRange(viewer, author);
        var posts = Enumerable.Range(0, 6).Select(_ => Post(author, "food")).ToArray();
        db.Posts.AddRange(posts);
        foreach (var post in posts.Take(4))
            db.Reactions.Add(new Reaction { Id = Guid.NewGuid(), ProfileId = viewer.Id,
                PostId = post.Id, Type = ReactionType.Like, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var reader = provider.GetRequiredService<IColdStartQueryReader>();
        Assert.Null(await reader.GetRankedPostIdsAsync(viewer.Id, default));
        Assert.NotEmpty((await reader.GetRankedPostIdsAsync(viewer.Id, 5, default))!);
        var collection = new Collection { Id = Guid.NewGuid(), ProfileId = viewer.Id, Title = "saved" };
        db.Collections.Add(collection);
        db.PostCollections.Add(new PostCollection { PostId = posts[0].Id, CollectionId = collection.Id, Collection = collection });
        await db.SaveChangesAsync();
        // Save + Like of the same post must remain four distinct positives.
        Assert.NotEmpty((await reader.GetRankedPostIdsAsync(viewer.Id, 5, default))!);
        db.Reactions.Add(new Reaction { Id = Guid.NewGuid(), ProfileId = viewer.Id,
            PostId = posts[4].Id, Type = ReactionType.Love, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        Assert.Null(await reader.GetRankedPostIdsAsync(viewer.Id, 5, default));
        viewer.IsBanned = true;
        await db.SaveChangesAsync();
        Assert.Empty((await reader.GetRankedPostIdsAsync(viewer.Id, 5, default))!);
        viewer.IsBanned = false;
        db.Reactions.RemoveRange(db.Reactions);
        await db.SaveChangesAsync();
        Assert.NotEmpty((await reader.GetRankedPostIdsAsync(viewer.Id, 5, default))!);
    }

    [Fact]
    public async Task AdaptiveLiveReaderRoutesFiltersPagesAndRevalidatesFallback()
    {
        using var provider = Provider();
        var db = provider.GetRequiredService<AppDbContext>();
        var viewer = new Profile { Id = Guid.NewGuid(), Username = "routing_viewer" };
        var author = new Profile { Id = Guid.NewGuid(), Username = "routing_author" };
        db.Profiles.AddRange(viewer, author);
        var posts = Enumerable.Range(0, 9).Select(_ => Post(author, "food")).ToArray();
        db.Posts.AddRange(posts);
        foreach (var post in posts.Take(4))
            db.Reactions.Add(new Reaction { Id = Guid.NewGuid(), ProfileId = viewer.Id,
                PostId = post.Id, Type = ReactionType.Like, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var ranks = posts.Select((p, i) => new Favi_BE.API.Application.ContentDiscovery.RankedExperimentPost(p.Id, -i)).ToArray();
        var artifact = new Favi_BE.API.Application.ContentDiscovery.ExperimentArtifact(2, "routing-test", DateTimeOffset.UtcNow,
            400, new string('a', 64), new string('b', 64), new string('c', 64),
            [new(viewer.Id, Ranks: new() { ["B1"] = ranks, ["R"] = ranks })],
            new() { ["B1"] = new("ease", 1, new() { ["value"] = System.Text.Json.JsonSerializer.SerializeToElement(1000) }, 0, null),
                ["R"] = new("ease", 5, new() { ["value"] = System.Text.Json.JsonSerializer.SerializeToElement(1000) }, 0, null) });
        var path = Path.Combine(Path.GetTempPath(), "favi-routing-" + Guid.NewGuid() + ".json");
        try
        {
            var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(artifact,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            File.WriteAllBytes(path, bytes);
            var snapshot = new Favi_BE.API.Application.ContentDiscovery.ExperimentalFeedSnapshot(
                Microsoft.Extensions.Options.Options.Create(new Favi_BE.API.Application.ContentDiscovery.ExperimentalFeedOptions
                { Enabled = true, ArtifactPath = path, ArtifactSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) }), TimeProvider.System);
            var fallback = System.Reflection.DispatchProxy.Create<IContentDiscoveryQueryReader, FallbackProxy>();
            ((FallbackProxy)(object)fallback).Rows = await provider.GetRequiredService<IColdStartQueryReader>()
                .LoadVisiblePostsAsync(viewer.Id, posts.Select(p => p.Id).ToArray(), default);
            var reader = new Favi_BE.API.Application.ContentDiscovery.ExperimentalFeedQueryReader(db,
                provider.GetRequiredService<IColdStartQueryReader>(), fallback, snapshot, TimeProvider.System);
            Assert.Equal("cold-fallback", (await reader.GetAsync(viewer.Id, "R", 1, 10, default))!.Source);
            Assert.Equal("B1", (await reader.GetAsync(viewer.Id, "B1", 1, 10, default))!.Source);
            db.Reactions.Add(new Reaction { Id = Guid.NewGuid(), ProfileId = viewer.Id,
                PostId = posts[4].Id, Type = ReactionType.Love, CreatedAt = DateTime.UtcNow });
            posts[5].Privacy = PrivacyLevel.Private;
            posts[6].IsNSFW = true;
            await db.SaveChangesAsync();
            var warm = (await reader.GetAsync(viewer.Id, "R", 1, 1, default))!;
            Assert.Equal("R", warm.Source);
            Assert.Equal(2, warm.TotalCount);
            Assert.Equal(posts[7].Id, Assert.Single(warm.Data).Id);
            Assert.Equal(posts[8].Id, Assert.Single((await reader.GetAsync(viewer.Id, "R", 2, 1, default))!.Data).Id);
            Assert.Equal("missing-model-fallback", (await reader.GetAsync(viewer.Id, "ease", 1, 10, default))!.Source);
            posts[7].IsArchived = true;
            posts[8].DeletedDayExpiredAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            var empty = (await reader.GetAsync(viewer.Id, "R", 1, 10, default))!;
            Assert.Equal("empty-model-fallback", empty.Source);
            Assert.Empty(empty.Data);
        }
        finally { File.Delete(path); }
    }

    private static ServiceProvider Provider(FakeGraph? graph = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(GetProfileInterestsQuery).Assembly));
        services.AddAuthModule();
        services.AddContentDiscoveryModule();
        if (graph != null) services.AddSingleton<IGraphCandidateSource>(graph);
        return services.BuildServiceProvider();
    }

    public class FallbackProxy : System.Reflection.DispatchProxy
    {
        public IReadOnlyList<Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels.PostReadModel> Rows { get; set; } = [];
        protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? args)
            => method?.Name == nameof(IContentDiscoveryQueryReader.GetNewsFeedAsync)
                ? Task.FromResult((Rows, Rows.Count)) : throw new NotSupportedException(method?.Name);
    }

    private static Post Post(Profile author, string topic)
    {
        var post = new Post { Id = Guid.NewGuid(), ProfileId = author.Id, Profile = author,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var tag = new Tag { Id = Guid.NewGuid(), Name = topic };
        post.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id, Post = post, Tag = tag });
        return post;
    }

    private sealed class FakeGraph : IGraphCandidateSource
    {
        public IReadOnlyList<GraphCandidate> Candidates { get; set; } = [];
        public bool Fail { get; set; }
        public int Limit { get; private set; }
        public Task<IReadOnlyList<GraphCandidate>> GetAsync(Guid userId, IReadOnlyList<string> topics,
            int limit, CancellationToken ct)
        {
            Limit = limit;
            return Fail ? Task.FromException<IReadOnlyList<GraphCandidate>>(new HttpRequestException())
                : Task.FromResult(Candidates);
        }
    }
}

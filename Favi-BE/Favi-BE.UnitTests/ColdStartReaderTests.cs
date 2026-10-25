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

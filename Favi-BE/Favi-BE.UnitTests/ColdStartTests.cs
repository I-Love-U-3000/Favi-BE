using Favi_BE.Modules.Auth.Application.Commands.SetProfileInterests;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileInterests;
using Favi_BE.Modules.Auth.Domain;
using Favi_BE.Modules.ContentDiscovery.Application.ColdStart;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.UnitTests;

public sealed class ColdStartTests
{
    private static ColdStartCandidate Candidate(int id, string topic, double score = 1, int author = 0)
        => new(new Guid(id, 0, 0, new byte[8]), new Guid(author == 0 ? id : author, 0, 0, new byte[8]), score, [topic]);

    [Fact]
    public void GeneralFeedBalancesTopicsInsteadOfPopularityOnly()
    {
        var posts = new[] { Candidate(1, "travel", 100), Candidate(2, "travel", 99),
            Candidate(3, "food", 1), Candidate(4, "art", 0.1) };
        var ranked = ColdStartRanker.Rank(posts, ["travel", "food", "art"]);
        Assert.Equal(new[] { posts[0].PostId, posts[2].PostId, posts[3].PostId }, ranked.Take(3));
    }

    [Fact]
    public void SelectedTopicsComeBeforeUnrelatedAndDeduplicateMultiTopicPosts()
    {
        var shared = Candidate(1, "food") with { Topics = new[] { "food", "travel" } };
        var travel = Candidate(2, "travel");
        var other = Candidate(3, "art", 999);
        var ranked = ColdStartRanker.Rank([other, shared, travel, shared], ["food", "travel"]);
        Assert.Equal(new[] { shared.PostId, travel.PostId, other.PostId }, ranked);
    }

    [Fact]
    public void DiversityCapRelaxesWhenOnlyOneAuthorHasContent()
    {
        var posts = Enumerable.Range(1, 5).Select(i => Candidate(i, "food", author: 1)).ToArray();
        Assert.Equal(5, ColdStartRanker.Rank(posts, ["food"]).Count);
    }

    [Fact]
    public void DiverseAuthorsAppearBeforeThirdPostFromSameAuthor()
    {
        var posts = new[] { Candidate(1, "food", 100, 1), Candidate(2, "food", 90, 1),
            Candidate(3, "food", 80, 1), Candidate(4, "food", 1, 2) };
        var ranked = ColdStartRanker.Rank(posts, ["food"]);
        Assert.Equal(posts[3].PostId, ranked[2]);
    }

    [Fact]
    public void EmptyMatchingTopicFallsBackAndNonFiniteScoresAreIgnored()
    {
        var post = Candidate(1, "art");
        Assert.Equal(new[] { post.PostId }, ColdStartRanker.Rank(
            [post, Candidate(2, "art", double.NaN)], ["food"]));
        Assert.Empty(ColdStartRanker.Rank([], ["food"]));
        Assert.Empty(ColdStartRanker.Rank([post], ["food"], 0));
    }

    [Fact]
    public void InterestsNormalizeAndValidate()
    {
        Assert.Equal(new[] { "food", "travel" }, InterestTopics.Normalize([" FOOD ", "travel", "food"]));
        Assert.Empty(InterestTopics.Normalize([]));
        Assert.Throws<ArgumentException>(() => InterestTopics.Normalize(["unknown"]));
        Assert.Throws<ArgumentException>(() => InterestTopics.Normalize(null));
        Assert.Throws<ArgumentException>(() => InterestTopics.Normalize(Enumerable.Repeat("food", 11)));
    }

    [Fact]
    public async Task NewUserCanReadDefaultsAndSavePreferencesWithVersion()
    {
        var store = new FakeStore();
        using var provider = Provider(store);
        var mediator = provider.GetRequiredService<IMediator>();
        var userId = Guid.NewGuid();
        var initial = await mediator.Send(new GetProfileInterestsQuery(userId));
        Assert.Equal(0, initial!.Version);
        Assert.Empty(initial.Topics);
        var saved = await mediator.Send(new SetProfileInterestsCommand(userId, ["food"], 0));
        Assert.Null(saved.Error);
        Assert.Equal(1, saved.Data!.Version);
        var stale = await mediator.Send(new SetProfileInterestsCommand(userId, ["art"], 0));
        Assert.Equal("CONCURRENCY_CONFLICT", stale.Error);
        Assert.Equal(new[] { "food" }, store.Value!.Topics);
    }

    [Fact]
    public async Task MissingProfileInvalidTopicsAndConcurrentSaveHaveExplicitErrors()
    {
        var store = new FakeStore { Exists = false };
        using var provider = Provider(store);
        var mediator = provider.GetRequiredService<IMediator>();
        var id = Guid.NewGuid();
        Assert.Null(await mediator.Send(new GetProfileInterestsQuery(id)));
        Assert.Equal("PROFILE_NOT_FOUND", (await mediator.Send(new SetProfileInterestsCommand(id, [], 0))).Error);
        store.Exists = true;
        Assert.Equal("INVALID_TOPICS", (await mediator.Send(new SetProfileInterestsCommand(id, ["bad"], 0))).Error);
        store.SaveSucceeds = false;
        Assert.Equal("CONCURRENCY_CONFLICT", (await mediator.Send(new SetProfileInterestsCommand(id, [], 0))).Error);
    }

    private static ServiceProvider Provider(FakeStore store)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProfileInterestsStore>(store);
        services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(GetProfileInterestsQuery).Assembly));
        return services.BuildServiceProvider();
    }

    private sealed class FakeStore : IProfileInterestsStore
    {
        public bool Exists { get; set; } = true;
        public bool SaveSucceeds { get; set; } = true;
        public ProfileInterests? Value { get; set; }
        public Task<bool> ProfileExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(Exists);
        public Task<ProfileInterestsData?> ReadAsync(Guid id, CancellationToken ct)
            => Task.FromResult(Value is null ? null : new ProfileInterestsData(Value.Topics, Value.Version));
        public Task<ProfileInterests?> FindForUpdateAsync(Guid id, CancellationToken ct) => Task.FromResult(Value);
        public void Add(ProfileInterests interests) => Value = interests;
        public Task<bool> TrySaveAsync(ProfileInterests interests, CancellationToken ct) => Task.FromResult(SaveSucceeds);
    }
}

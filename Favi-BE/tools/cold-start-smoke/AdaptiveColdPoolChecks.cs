using Favi_BE.API.DependencyInjection;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileInterests;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static class AdaptiveColdPoolChecks
{
    public static async Task RunAsync(string connection, Action<bool, string> check)
    {
        // The caller only supplies its dedicated smoke database. Roll back all
        // fixture changes, including temporary isolation from the existing catalog.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connection));
        services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(GetProfileInterestsQuery).Assembly));
        services.AddAuthModule();
        services.AddContentDiscoveryModule();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Posts.ExecuteUpdateAsync(p => p.SetProperty(x => x.IsArchived, true));
            var viewer = new Profile { Id = Guid.NewGuid(), Username = "pool_" + Guid.NewGuid().ToString("N") };
            var author = new Profile { Id = Guid.NewGuid(), Username = "pool_" + Guid.NewGuid().ToString("N") };
            db.Profiles.AddRange(viewer, author);
            var now = DateTime.UtcNow;
            var posts = Enumerable.Range(0, 64).Select(i => new Post
            {
                Id = Guid.NewGuid(), ProfileId = author.Id, Profile = author,
                Caption = "adaptive-pool-fixture", CreatedAt = now.AddMinutes(-i), UpdatedAt = now
            }).ToArray();
            db.Posts.AddRange(posts);
            db.Reactions.AddRange(
                new Reaction { Id = Guid.NewGuid(), ProfileId = viewer.Id, PostId = posts[0].Id, Type = ReactionType.Like, CreatedAt = now },
                new Reaction { Id = Guid.NewGuid(), ProfileId = viewer.Id, PostId = posts[1].Id, Type = ReactionType.Love, CreatedAt = now });
            var collection = new Collection { Id = Guid.NewGuid(), ProfileId = viewer.Id, Title = "pool_saved", CreatedAt = now, UpdatedAt = now };
            db.Collections.Add(collection);
            foreach (var post in new[] { posts[0], posts[2], posts[3] })
                db.PostCollections.Add(new PostCollection { PostId = post.Id, CollectionId = collection.Id, Collection = collection });
            await db.SaveChangesAsync();
            var reader = scope.ServiceProvider.GetRequiredService<IColdStartQueryReader>();
            var ids = await reader.GetRankedPostIdsAsync(viewer.Id, 5, default);
            check(ids is not null && ids.Count == 60 && ids.ToHashSet().SetEquals(posts.Skip(4).Select(p => p.Id)),
                "adaptive cold PostgreSQL pool keeps 60 unseen posts after Like/Love/save UNION before LIMIT");
            check(await reader.GetRankedPostIdsAsync(viewer.Id, default) is null,
                "production threshold1 still routes positive-history viewer warm");
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}

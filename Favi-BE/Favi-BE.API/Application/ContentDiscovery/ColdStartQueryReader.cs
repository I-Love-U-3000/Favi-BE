using Favi_BE.Data;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileInterests;
using Favi_BE.Modules.Auth.Domain;
using Favi_BE.Modules.ContentDiscovery.Application.ColdStart;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Application.ContentDiscovery;

internal sealed class ColdStartQueryReader(
    AppDbContext db, IMediator mediator, IGraphCandidateSource graph, ILogger<ColdStartQueryReader> logger)
    : IColdStartQueryReader
{
    public async Task<IReadOnlyList<Guid>?> GetRankedPostIdsAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (!await db.Profiles.AsNoTracking().AnyAsync(p => p.Id == userId &&
                (!p.IsBanned || (p.BannedUntil != null && p.BannedUntil <= now)), ct)) return [];

        var hasLikes = await db.Reactions.AsNoTracking().AnyAsync(r => r.ProfileId == userId &&
            r.PostId != null && (r.Type == ReactionType.Like || r.Type == ReactionType.Love), ct);
        var hasSaves = await db.PostCollections.AsNoTracking().AnyAsync(p => p.Collection.ProfileId == userId, ct);
        if (hasLikes || hasSaves) return null;

        // Host bridges Auth query to ContentDiscovery; neither module references the other.
        var preferences = await mediator.Send(new GetProfileInterestsQuery(userId), ct);
        IReadOnlyList<string> topics = preferences?.Topics.Length > 0 ? preferences.Topics : InterestTopics.All;

        IReadOnlyList<GraphCandidate> graphCandidates = [];
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(TimeSpan.FromMilliseconds(250));
            try
            {
                graphCandidates = await graph.GetAsync(userId, topics, 300, timeout.Token).WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("Cold-start graph timeout; using catalog fallback");
            }
            catch (HttpRequestException)
            {
                logger.LogWarning("Cold-start graph unavailable; using catalog fallback");
            }
        }

        // Authoritative safety/visibility comes from Favi, not the future graph service.
        var eligible = EligiblePosts(userId);

        var engineIds = graphCandidates.Where(c => c.PostId != Guid.Empty && double.IsFinite(c.Score))
            .OrderByDescending(c => c.Score).ThenBy(c => c.PostId).Select(c => c.PostId).Distinct().Take(300).ToArray();
        var engineOrder = engineIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var pool = new List<ColdStartCandidate>();
        if (engineIds.Length > 0)
        {
            var rows = await eligible.Where(p => engineIds.Contains(p.Id)).Select(p => new
            {
                p.Id, p.ProfileId, Topics = p.PostTags.Select(t => t.Tag.Name.ToLower()).ToArray()
            }).ToListAsync(ct);
            // Rank fusion: preserve source order, never mix raw graph scores with popularity.
            pool.AddRange(rows.Select(p => new ColdStartCandidate(p.Id, p.ProfileId,
                1000 - engineOrder[p.Id], p.Topics)));
        }

        // Small per-topic pools ensure a popular topic cannot crowd every other topic out.
        foreach (var topic in topics)
        {
            var rows = await eligible.Where(p => p.PostTags.Any(t => t.Tag.Name.ToLower() == topic))
                .OrderByDescending(p => p.Reactions.Count + 2 * p.Comments.Count)
                .ThenByDescending(p => p.CreatedAt).ThenBy(p => p.Id).Take(20)
                .Select(p => new { p.Id, p.ProfileId, Topics = p.PostTags.Select(t => t.Tag.Name.ToLower()).ToArray() })
                .ToListAsync(ct);
            pool.AddRange(rows.Select((p, i) => new ColdStartCandidate(p.Id, p.ProfileId, -i, p.Topics)));
        }
        // Untagged/rare catalogs still produce a feed when selected topics have no matches.
        var general = await eligible.OrderByDescending(p => p.Reactions.Count + 2 * p.Comments.Count)
            .ThenByDescending(p => p.CreatedAt).ThenBy(p => p.Id).Take(60)
            .Select(p => new { p.Id, p.ProfileId, Topics = p.PostTags.Select(t => t.Tag.Name.ToLower()).ToArray() })
            .ToListAsync(ct);
        pool.AddRange(general.Select((p, i) => new ColdStartCandidate(p.Id, p.ProfileId, -100 - i, p.Topics)));
        return ColdStartRanker.Rank(pool, topics);
    }

    private IQueryable<Favi_BE.Models.Entities.Post> EligiblePosts(Guid userId)
    {
        var now = DateTime.UtcNow;
        return db.Posts.AsNoTracking().Where(p => p.ProfileId != userId &&
            p.Privacy == PrivacyLevel.Public && p.Profile.PrivacyLevel == PrivacyLevel.Public &&
            !p.IsNSFW && !p.IsArchived && p.DeletedDayExpiredAt == null &&
            (!p.Profile.IsBanned || (p.Profile.BannedUntil != null && p.Profile.BannedUntil <= now)));
    }

    public async Task<IReadOnlyList<PostReadModel>> LoadVisiblePostsAsync(
        Guid userId, IReadOnlyList<Guid> postIds, CancellationToken ct)
    {
        if (postIds.Count == 0) return [];
        var ids = postIds.Distinct().Take(300).ToArray();
        var now = DateTime.UtcNow;
        // Projection rechecks current visibility without loading comments or N+1 privacy queries.
        return await EligiblePosts(userId).Where(p => ids.Contains(p.Id) && db.Profiles.Any(v =>
                v.Id == userId && (!v.IsBanned || (v.BannedUntil != null && v.BannedUntil <= now))))
            .Select(p => new PostReadModel(p.Id, p.ProfileId, p.Caption, p.CreatedAt, p.UpdatedAt,
                (int)p.Privacy,
                p.PostMedias.OrderBy(m => m.Position).Select(m => new PostMediaReadModel(
                    m.Id, p.Id, m.Url, m.PublicId, m.Width, m.Height, m.Format, m.Position, m.ThumbnailUrl)).ToList(),
                p.PostTags.Select(t => new TagReadModel(t.TagId, t.Tag.Name)).ToList(),
                p.LocationName == null ? null : new PostLocationReadModel(
                    p.LocationName, p.LocationFullAddress, p.LocationLatitude, p.LocationLongitude),
                p.IsNSFW, p.Comments.Count, p.Version,
                p.Profile.Username, p.Profile.DisplayName, p.Profile.AvatarUrl))
            .ToListAsync(ct);
    }
}

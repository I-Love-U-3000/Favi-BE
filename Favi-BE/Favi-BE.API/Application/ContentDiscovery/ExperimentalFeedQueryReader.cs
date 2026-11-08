using Favi_BE.Data;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Application.ContentDiscovery;

public sealed class ExperimentalFeedQueryReader(AppDbContext db, IColdStartQueryReader cold,
    IContentDiscoveryQueryReader regular, ExperimentalFeedSnapshot snapshot, TimeProvider time)
    : IExperimentalFeedQueryReader
{
    public async Task<ExperimentalFeedResult?> GetAsync(Guid userId, string method, int page, int pageSize, CancellationToken ct)
    {
        if (!snapshot.Enabled) return null;
        if (method is not ("ease" or "ppr" or "B1" or "R") || page is < 1 or > 10000 || pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(page), "Invalid experiment method/pagination");
        var coldIds = await cold.GetRankedPostIdsAsync(userId, snapshot.GetThreshold(method), ct);
        string source;
        IReadOnlyList<PostReadModel> eligible;
        if (coldIds is not null)
        {
            source = "cold-fallback";
            eligible = await Load(coldIds);
        }
        else
        {
            var ranked = snapshot.Get(userId, method);
            source = snapshot.IsStale(time.GetUtcNow()) ? "stale-model-fallback" : ranked is null ? "missing-model-fallback" : method;
            eligible = source == method ? await Load(ranked!) : [];
            if (source != method || eligible.Count == 0)
            {
                if (source == method) source = "empty-model-fallback";
                var fallback = await regular.GetNewsFeedAsync(userId, 1, 100, ct);
                eligible = await Load(fallback.Items.Select(p => p.Id).ToArray());
            }
        }
        var offset = (page - 1) * pageSize;
        var rows = eligible.Skip(offset).Take(pageSize).ToArray();
        var authors = rows.GroupBy(p => p.AuthorProfileId).Select(g => g.Count()).ToArray();
        var now = time.GetUtcNow();
        return new(method, source, snapshot.Version, page, pageSize, eligible.Count,
            offset + rows.Length < eligible.Count, rows,
            rows.Length == 0 ? 0 : (double)authors.Length / rows.Length,
            rows.Length == 0 ? 0 : (double)authors.Max() / rows.Length,
            rows.Length == 0 ? null : rows.Average(p => Math.Max(0,
                (now - new DateTimeOffset(DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc))).TotalHours)));

        async Task<IReadOnlyList<PostReadModel>> Load(IReadOnlyList<Guid> ranked)
        {
            var ids = ranked.Distinct().Take(300).ToArray();
            if (ids.Length == 0) return [];
            var liked = await db.Reactions.AsNoTracking().Where(r => r.ProfileId == userId && r.PostId.HasValue &&
                ids.Contains(r.PostId.Value) && (r.Type == ReactionType.Like || r.Type == ReactionType.Love))
                .Select(r => r.PostId!.Value).ToListAsync(ct);
            var saved = await db.PostCollections.AsNoTracking().Where(p => p.Collection.ProfileId == userId && ids.Contains(p.PostId))
                .Select(p => p.PostId).ToListAsync(ct);
            var seen = liked.Concat(saved).ToHashSet();
            var visible = await cold.LoadVisiblePostsAsync(userId, ids.Where(id => !seen.Contains(id)).ToArray(), ct);
            var map = visible.ToDictionary(p => p.Id);
            return ids.Where(map.ContainsKey).Select(id => map[id]).ToArray();
        }
    }
}

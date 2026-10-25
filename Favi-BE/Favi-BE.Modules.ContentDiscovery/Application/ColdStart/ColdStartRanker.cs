namespace Favi_BE.Modules.ContentDiscovery.Application.ColdStart;

public sealed record ColdStartCandidate(Guid PostId, Guid AuthorId, double Score, IReadOnlyList<string> Topics);

public static class ColdStartRanker
{
    /// <summary>Round-robin topics, dedup posts, prefer diverse authors; deterministic tie-breaks.</summary>
    public static IReadOnlyList<Guid> Rank(
        IEnumerable<ColdStartCandidate> candidates, IReadOnlyList<string> topics, int limit = 300)
    {
        if (limit <= 0) return [];
        var pool = candidates.Where(c => c.PostId != Guid.Empty && double.IsFinite(c.Score))
            .OrderByDescending(c => c.Score).ThenBy(c => c.PostId)
            .DistinctBy(c => c.PostId).ToArray();
        var selected = new HashSet<Guid>();
        var authors = new Dictionary<Guid, int>();
        var result = new List<Guid>();

        void Add(ColdStartCandidate candidate)
        {
            selected.Add(candidate.PostId);
            authors[candidate.AuthorId] = authors.GetValueOrDefault(candidate.AuthorId) + 1;
            result.Add(candidate.PostId);
        }

        void RoundRobin(int authorCap)
        {
            var queues = topics.Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(t => new Queue<ColdStartCandidate>(pool.Where(p =>
                    p.Topics.Contains(t, StringComparer.OrdinalIgnoreCase) && !selected.Contains(p.PostId))))
                .ToArray();
            bool progress;
            do
            {
                progress = false;
                foreach (var queue in queues)
                {
                    while (queue.TryDequeue(out var candidate))
                    {
                        if (selected.Contains(candidate.PostId) || authors.GetValueOrDefault(candidate.AuthorId) >= authorCap)
                            continue;
                        Add(candidate);
                        progress = true;
                        break;
                    }
                    if (result.Count >= limit) return;
                }
            } while (progress);
        }

        RoundRobin(2);
        if (result.Count < limit) RoundRobin(int.MaxValue);
        // No matching topics or topic pools exhausted: still return useful content.
        foreach (var candidate in pool)
        {
            if (result.Count >= limit) break;
            if (!selected.Contains(candidate.PostId)) Add(candidate);
        }
        return result;
    }
}

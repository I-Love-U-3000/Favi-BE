namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts;

public sealed record GraphCandidate(Guid PostId, double Score);

/// <summary>
/// Provisional graph-engine boundary. Future adapter maps engine IDs/scores here.
/// Never return entities or assume engine scores are comparable to catalog popularity.
/// Implementations must honor cancellation; Favi rechecks all IDs against its catalog.
/// </summary>
public interface IGraphCandidateSource
{
    Task<IReadOnlyList<GraphCandidate>> GetAsync(
        Guid userId, IReadOnlyList<string> topics, int limit, CancellationToken ct);
}

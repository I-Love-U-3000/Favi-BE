using Favi_BE.Modules.ContentDiscovery.Application.Contracts;

namespace Favi_BE.API.Application.ContentDiscovery;

/// <summary>
/// Graph engine is NOT implemented. Replace this DI registration with an adapter
/// once its real contract is available. No fake predictions are served to users.
/// </summary>
internal sealed class NoGraphCandidateSource : IGraphCandidateSource
{
    public Task<IReadOnlyList<GraphCandidate>> GetAsync(
        Guid userId, IReadOnlyList<string> topics, int limit, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GraphCandidate>>([]);
}

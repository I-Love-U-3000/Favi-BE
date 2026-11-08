namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts;

public interface IColdStartQueryReader
{
    /// <summary>Null means warm user; an empty list means cold user without eligible content.</summary>
    Task<IReadOnlyList<Guid>?> GetRankedPostIdsAsync(Guid userId, CancellationToken ct);
    /// <summary>Experimental threshold; counts distinct current Like/Love or saved post IDs.</summary>
    Task<IReadOnlyList<Guid>?> GetRankedPostIdsAsync(Guid userId, int positiveThreshold, CancellationToken ct);
    Task<IReadOnlyList<ReadModels.PostReadModel>> LoadVisiblePostsAsync(
        Guid userId, IReadOnlyList<Guid> postIds, CancellationToken ct);
}

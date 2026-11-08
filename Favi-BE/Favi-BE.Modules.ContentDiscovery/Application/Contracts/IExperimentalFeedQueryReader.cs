using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts;

public sealed record ExperimentalFeedResult(
    string Method, string Source, string SnapshotVersion, int Page, int PageSize,
    int TotalCount, bool HasNext, IReadOnlyList<PostReadModel> Data,
    double AuthorDiversity, double MaxAuthorShare, double? MeanAgeHours);

public interface IExperimentalFeedQueryReader
{
    Task<ExperimentalFeedResult?> GetAsync(Guid userId, string method, int page, int pageSize, CancellationToken ct);
}

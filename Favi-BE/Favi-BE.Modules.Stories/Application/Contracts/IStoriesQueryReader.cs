using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Stories.Application.Contracts;

public interface IStoriesQueryReader
{
    Task<StoryReadModel?> GetStoryByIdAsync(Guid storyId, Guid? viewerId, CancellationToken ct = default);
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);
    Task<IReadOnlyList<StoryReadModel>> GetActiveStoriesByProfileAsync(Guid profileId, Guid? viewerId, CancellationToken ct = default);
    Task<IReadOnlyList<StoryReadModel>> GetViewableStoriesAsync(Guid viewerId, CancellationToken ct = default);
    Task<IReadOnlyList<StoryReadModel>> GetArchivedStoriesAsync(Guid profileId, CancellationToken ct = default);
    Task<IReadOnlyList<StoryViewerReadModel>> GetViewersAsync(Guid storyId, Guid requesterId, CancellationToken ct = default);
    Task<int> GetActiveStoryCountAsync(Guid profileId, CancellationToken ct = default);
}

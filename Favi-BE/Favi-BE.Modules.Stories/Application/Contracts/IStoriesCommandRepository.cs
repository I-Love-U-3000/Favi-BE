using Favi_BE.Modules.Stories.Application.Contracts.WriteModels;

namespace Favi_BE.Modules.Stories.Application.Contracts;

public interface IStoriesCommandRepository
{
    Task AddStoryAsync(StoryWriteData data, CancellationToken ct = default);
    Task<StoryWriteData?> GetStoryForWriteAsync(Guid storyId, CancellationToken ct = default);
    Task SetArchivedAsync(Guid storyId, bool isArchived, CancellationToken ct = default);
    Task RemoveStoryAsync(Guid storyId, CancellationToken ct = default);

    // Returns false if the viewer already recorded a view (idempotent).
    Task<bool> RecordViewAsync(Guid storyId, Guid viewerProfileId, CancellationToken ct = default);

    Task<IReadOnlyList<StoryWriteData>> GetExpiredStoriesAsync(CancellationToken ct = default);

    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);
}

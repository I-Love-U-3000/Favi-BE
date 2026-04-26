using Favi_BE.Interfaces;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.WriteModels;
using Favi_BE.Modules.Stories.Domain;
using LegacyPrivacy = Favi_BE.Models.Enums.PrivacyLevel;

namespace Favi_BE.API.Application.Stories;

internal sealed class StoriesCommandRepositoryAdapter : IStoriesCommandRepository
{
    private readonly IUnitOfWork _uow;

    public StoriesCommandRepositoryAdapter(IUnitOfWork uow) => _uow = uow;

    public async Task AddStoryAsync(StoryWriteData data, CancellationToken ct = default)
        => await _uow.Stories.AddAsync(new Story
        {
            Id = data.Id,
            ProfileId = data.ProfileId,
            MediaUrl = data.MediaUrl,
            MediaPublicId = data.MediaPublicId,
            MediaWidth = data.MediaWidth,
            MediaHeight = data.MediaHeight,
            MediaFormat = data.MediaFormat,
            ThumbnailUrl = data.ThumbnailUrl,
            Privacy = MapPrivacy(data.Privacy),
            IsArchived = data.IsArchived,
            IsNSFW = data.IsNSFW,
            CreatedAt = data.CreatedAt,
            ExpiresAt = data.ExpiresAt
        });

    public async Task<StoryWriteData?> GetStoryForWriteAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        return story is null ? null : MapStory(story);
    }

    public async Task SetArchivedAsync(Guid storyId, bool isArchived, CancellationToken ct = default)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        if (story is null) return;

        story.IsArchived = isArchived;
        _uow.Stories.Update(story);
    }

    public async Task RemoveStoryAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        if (story is not null)
            _uow.Stories.Remove(story);
    }

    public async Task<bool> RecordViewAsync(Guid storyId, Guid viewerProfileId, CancellationToken ct = default)
    {
        if (await _uow.StoryViews.HasViewedAsync(storyId, viewerProfileId))
            return false;

        await _uow.StoryViews.AddAsync(new StoryView
        {
            StoryId = storyId,
            ViewerProfileId = viewerProfileId,
            ViewedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<IReadOnlyList<StoryWriteData>> GetExpiredStoriesAsync(CancellationToken ct = default)
    {
        var stories = await _uow.Stories.GetExpiredStoriesAsync();
        return stories.Select(MapStory).ToList();
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    public async Task SaveAsync(CancellationToken ct = default)
        => await _uow.CompleteAsync();

    private static LegacyPrivacy MapPrivacy(StoryPrivacy p) => (LegacyPrivacy)(int)p;

    private static StoryWriteData MapStory(Story s) => new(
        s.Id, s.ProfileId, s.MediaUrl, s.MediaPublicId,
        s.MediaWidth, s.MediaHeight, s.MediaFormat, s.ThumbnailUrl,
        (StoryPrivacy)(int)s.Privacy, s.IsArchived, s.IsNSFW,
        s.CreatedAt, s.ExpiresAt
    );
}

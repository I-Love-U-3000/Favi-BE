using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Entities;
using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;
using Favi_BE.Modules.Stories.Domain;
using LegacyPrivacy = Favi_BE.Models.Enums.PrivacyLevel;

namespace Favi_BE.API.Application.Stories;

internal sealed class StoriesQueryReaderAdapter : IStoriesQueryReader
{
    private readonly IUnitOfWork _uow;
    private readonly IPrivacyGuard _privacy;

    public StoriesQueryReaderAdapter(IUnitOfWork uow, IPrivacyGuard privacy)
    {
        _uow = uow;
        _privacy = privacy;
    }

    public async Task<StoryReadModel?> GetStoryByIdAsync(Guid storyId, Guid? viewerId, CancellationToken ct = default)
    {
        var story = await _uow.Stories.GetActiveStoryWithDetailsAsync(storyId);
        if (story is null) return null;

        if (!await _privacy.CanViewStoryAsync(story, viewerId))
            return null;

        return await MapAsync(story, viewerId);
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    public async Task<IReadOnlyList<StoryReadModel>> GetActiveStoriesByProfileAsync(
        Guid profileId, Guid? viewerId, CancellationToken ct = default)
    {
        var stories = await _uow.Stories.GetActiveStoriesByProfileIdAsync(profileId);

        var result = new List<StoryReadModel>();
        foreach (var story in stories)
        {
            if (await _privacy.CanViewStoryAsync(story, viewerId))
                result.Add(await MapAsync(story, viewerId));
        }
        return result;
    }

    public async Task<IReadOnlyList<StoryReadModel>> GetViewableStoriesAsync(
        Guid viewerId, CancellationToken ct = default)
    {
        var stories = await _uow.Stories.GetViewableStoriesAsync(viewerId);

        var result = new List<StoryReadModel>();
        foreach (var story in stories)
        {
            if (await _privacy.CanViewStoryAsync(story, viewerId))
                result.Add(await MapAsync(story, viewerId));
        }
        return result;
    }

    public async Task<IReadOnlyList<StoryReadModel>> GetArchivedStoriesAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var stories = await _uow.Stories.GetArchivedStoriesByProfileIdAsync(profileId);

        var result = new List<StoryReadModel>();
        foreach (var story in stories)
            result.Add(await MapAsync(story, profileId));

        return result;
    }

    public async Task<IReadOnlyList<StoryViewerReadModel>> GetViewersAsync(
        Guid storyId, Guid requesterId, CancellationToken ct = default)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        if (story is null || story.ProfileId != requesterId)
            throw new UnauthorizedAccessException("Only the story owner can see viewers.");

        var views = await _uow.StoryViews.GetViewersByStoryIdAsync(storyId);
        return views.Select(sv => new StoryViewerReadModel(
            sv.Viewer.Id,
            sv.Viewer.Username,
            sv.Viewer.DisplayName,
            sv.Viewer.AvatarUrl,
            sv.ViewedAt
        )).ToList();
    }

    public async Task<int> GetActiveStoryCountAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Stories.CountActiveStoriesByProfileIdAsync(profileId);

    private async Task<StoryReadModel> MapAsync(Story story, Guid? viewerId)
    {
        var viewCount = await _uow.StoryViews.GetViewCountAsync(story.Id);
        var hasViewed = viewerId.HasValue && await _uow.StoryViews.HasViewedAsync(story.Id, viewerId.Value);

        return new StoryReadModel(
            story.Id,
            story.ProfileId,
            story.Profile.Username,
            story.Profile.AvatarUrl,
            story.MediaUrl ?? throw new InvalidOperationException($"Story '{story.Id}' has no MediaUrl — data integrity violation."),
            story.ThumbnailUrl,
            story.CreatedAt,
            story.ExpiresAt,
            (StoryPrivacy)(int)story.Privacy,
            story.IsArchived,
            story.IsNSFW,
            viewCount,
            hasViewed);
    }
}

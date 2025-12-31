using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Services
{
    public class StoryService : IStoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICloudinaryService _cloudinary;
        private readonly IPrivacyGuard _privacy;
        private readonly INSFWService _nsfwService;

        public StoryService(IUnitOfWork uow, ICloudinaryService cloudinary, IPrivacyGuard privacy, INSFWService nsfwService)
        {
            _uow = uow;
            _cloudinary = cloudinary;
            _privacy = privacy;
            _nsfwService = nsfwService;
        }

        public Task<Story?> GetEntityAsync(Guid storyId) => _uow.Stories.GetByIdAsync(storyId);

        public async Task<StoryResponse?> GetByIdAsync(Guid storyId, Guid? currentUserId)
        {
            var story = await _uow.Stories.GetActiveStoryWithDetailsAsync(storyId);
            if (story == null) return null;

            // Check privacy
            if (!await _privacy.CanViewStoryAsync(story, currentUserId))
                return null;

            return await MapToResponseAsync(story, currentUserId);
        }

        public async Task<StoryResponse> CreateAsync(Guid profileId, IFormFile mediaFile, PrivacyLevel privacy)
        {
            // Validate image file
            if (mediaFile == null || mediaFile.Length == 0)
                throw new ArgumentException("Media file is required");

            if (!mediaFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only image files are supported for stories");

            // Upload to Cloudinary
            var uploaded = await _cloudinary.UploadAsyncOrThrow(mediaFile, folder: "favi_stories");

            // Create story (expires in 24 hours)
            var now = DateTime.UtcNow;
            var story = new Story
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                MediaUrl = uploaded.Url,
                MediaPublicId = uploaded.PublicId,
                MediaWidth = uploaded.Width,
                MediaHeight = uploaded.Height,
                MediaFormat = uploaded.Format,
                ThumbnailUrl = uploaded.ThumbnailUrl,
                Privacy = privacy,
                IsArchived = false,
                CreatedAt = now,
                ExpiresAt = now.AddHours(24)
            };

            await _uow.Stories.AddAsync(story);
            await _uow.CompleteAsync();

            // Check NSFW content
            if (_nsfwService.IsEnabled())
            {
                try
                {
                    // Reload story with all details for NSFW check
                    var storyForNSFW = await _uow.Stories.GetActiveStoryWithDetailsAsync(story.Id);
                    if (storyForNSFW != null)
                    {
                        var post = new Post
                        {
                            Id = Guid.NewGuid(), // Temp ID for NSFW check
                            Caption = "", // Stories don't have captions
                            PostMedias = new List<PostMedia>
                            {
                                new PostMedia
                                {
                                    Url = storyForNSFW.MediaUrl,
                                    Position = 0
                                }
                            }
                        };

                        storyForNSFW.IsNSFW = await _nsfwService.CheckPostAsync(post);
                        _uow.Stories.Update(storyForNSFW);
                        await _uow.CompleteAsync();
                    }
                }
                catch
                {
                    // Swallow - errors logged in NSFWService
                }
            }

            // Reload story from database with Profile navigation property included
            var storyWithProfile = await _uow.Stories.GetActiveStoryWithDetailsAsync(story.Id);
            if (storyWithProfile == null)
            {
                // Fallback: use the original story if reload fails
                storyWithProfile = story;
            }

            return await MapToResponseAsync(storyWithProfile, profileId);
        }

        public async Task<bool> ArchiveAsync(Guid storyId, Guid requesterId)
        {
            var story = await _uow.Stories.GetByIdAsync(storyId);
            if (story == null || story.ProfileId != requesterId) return false;
            if (story.IsArchived) return true; // Already archived

            story.IsArchived = true;
            _uow.Stories.Update(story);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(Guid storyId, Guid requesterId)
        {
            var story = await _uow.Stories.GetByIdAsync(storyId);
            if (story == null || story.ProfileId != requesterId) return false;

            // Delete from Cloudinary
            if (!string.IsNullOrWhiteSpace(story.MediaPublicId))
            {
                await _cloudinary.TryDeleteAsync(story.MediaPublicId);
            }

            // Delete story views (cascade will handle this)
            _uow.Stories.Remove(story);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<IEnumerable<StoryResponse>> GetActiveStoriesByProfileAsync(Guid profileId, Guid? viewerId)
        {
            var stories = await _uow.Stories.GetActiveStoriesByProfileIdAsync(profileId);

            var result = new List<StoryResponse>();
            foreach (var story in stories)
            {
                if (await _privacy.CanViewStoryAsync(story, viewerId))
                    result.Add(await MapToResponseAsync(story, viewerId));
            }

            return result;
        }

        public async Task<IEnumerable<StoryResponse>> GetViewableStoriesAsync(Guid viewerId)
        {
            var stories = await _uow.Stories.GetViewableStoriesAsync(viewerId);

            // Apply privacy filtering
            var filteredStories = new List<Story>();
            foreach (var story in stories)
            {
                if (await _privacy.CanViewStoryAsync(story, viewerId))
                    filteredStories.Add(story);
            }

            var responses = new List<StoryResponse>();
            foreach (var story in filteredStories)
            {
                responses.Add(await MapToResponseAsync(story, viewerId));
            }

            return responses;
        }

        public async Task<IEnumerable<StoryResponse>> GetArchivedStoriesAsync(Guid profileId)
        {
            var stories = await _uow.Stories.GetArchivedStoriesByProfileIdAsync(profileId);

            var responses = new List<StoryResponse>();
            foreach (var story in stories)
            {
                responses.Add(await MapToResponseAsync(story, profileId));
            }

            return responses;
        }

        public async Task<bool> RecordViewAsync(Guid storyId, Guid viewerId)
        {
            var story = await _uow.Stories.GetByIdAsync(storyId);
            if (story == null) return false;

            // Don't track own views
            if (story.ProfileId == viewerId) return false;

            return await _uow.StoryViews.RecordViewAsync(storyId, viewerId);
        }

        public async Task<IEnumerable<StoryViewerResponse>> GetViewersAsync(Guid storyId, Guid requesterId)
        {
            var story = await _uow.Stories.GetByIdAsync(storyId);
            if (story == null || story.ProfileId != requesterId)
                throw new UnauthorizedAccessException("Only story owner can see viewers");

            var views = await _uow.StoryViews.GetViewersByStoryIdAsync(storyId);

            return views.Select(sv => new StoryViewerResponse(
                sv.Viewer.Id,
                sv.Viewer.Username,
                sv.Viewer.DisplayName,
                sv.Viewer.AvatarUrl,
                sv.ViewedAt
            ));
        }

        public async Task<int> GetActiveStoryCountAsync(Guid profileId)
        {
            return await _uow.Stories.CountActiveStoriesByProfileIdAsync(profileId);
        }

        private async Task<StoryResponse> MapToResponseAsync(Story story, Guid? currentUserId)
        {
            // Get view count
            var viewCount = await _uow.StoryViews.GetViewCountAsync(story.Id);

            // Check if current user has viewed
            bool hasViewed = false;
            if (currentUserId.HasValue)
            {
                hasViewed = await _uow.StoryViews.HasViewedAsync(story.Id, currentUserId.Value);
            }

            return new StoryResponse(
                Id: story.Id,
                ProfileId: story.ProfileId,
                ProfileUsername: story.Profile.Username,
                ProfileAvatarUrl: story.Profile.AvatarUrl,
                MediaUrl: story.MediaUrl,
                ThumbnailUrl: story.ThumbnailUrl,
                CreatedAt: story.CreatedAt,
                ExpiresAt: story.ExpiresAt,
                Privacy: story.Privacy,
                IsArchived: story.IsArchived,
                IsNSFW: story.IsNSFW,
                ViewCount: viewCount,
                HasViewed: hasViewed
            );
        }
    }
}

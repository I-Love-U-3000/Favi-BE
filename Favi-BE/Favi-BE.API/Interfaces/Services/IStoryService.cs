using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Services
{
    public interface IStoryService
    {
        Task<Story?> GetEntityAsync(Guid storyId);
        Task<StoryResponse?> GetByIdAsync(Guid storyId, Guid? currentUserId);
        Task<StoryResponse> CreateAsync(Guid profileId, IFormFile mediaFile, PrivacyLevel privacy);
        Task<bool> ArchiveAsync(Guid storyId, Guid requesterId);
        Task<bool> DeleteAsync(Guid storyId, Guid requesterId);
        Task<IEnumerable<StoryResponse>> GetActiveStoriesByProfileAsync(Guid profileId, Guid? viewerId);
        Task<IEnumerable<StoryResponse>> GetViewableStoriesAsync(Guid viewerId);
        Task<IEnumerable<StoryResponse>> GetArchivedStoriesAsync(Guid profileId);
        Task<bool> RecordViewAsync(Guid storyId, Guid viewerId);
        Task<IEnumerable<StoryViewerResponse>> GetViewersAsync(Guid storyId, Guid requesterId);
        Task<int> GetActiveStoryCountAsync(Guid profileId);
    }
}

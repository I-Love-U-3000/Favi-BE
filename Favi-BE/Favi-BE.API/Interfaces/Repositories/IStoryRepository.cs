using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IStoryRepository : IGenericRepository<Story>
    {
        Task<IEnumerable<Story>> GetActiveStoriesByProfileIdAsync(Guid profileId);
        Task<Story?> GetActiveStoryWithDetailsAsync(Guid storyId);
        Task<IEnumerable<Story>> GetViewableStoriesAsync(Guid viewerProfileId);
        Task<IEnumerable<Story>> GetStoriesByProfileIdAsync(Guid profileId, bool includeArchived = false);
        Task<IEnumerable<Story>> GetArchivedStoriesByProfileIdAsync(Guid profileId);
        Task<int> CountActiveStoriesByProfileIdAsync(Guid profileId);
        Task<IEnumerable<Story>> GetExpiredStoriesAsync();
    }
}

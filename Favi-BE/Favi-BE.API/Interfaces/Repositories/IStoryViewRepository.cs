using Favi_BE.Models.Entities.JoinTables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IStoryViewRepository : IGenericRepository<StoryView>
    {
        Task<bool> HasViewedAsync(Guid storyId, Guid viewerProfileId);
        Task<IEnumerable<StoryView>> GetViewersByStoryIdAsync(Guid storyId);
        Task<bool> RecordViewAsync(Guid storyId, Guid viewerProfileId);
        Task<int> GetViewCountAsync(Guid storyId);
        Task<IEnumerable<StoryView>> GetViewsByViewerAsync(Guid viewerProfileId);
    }
}

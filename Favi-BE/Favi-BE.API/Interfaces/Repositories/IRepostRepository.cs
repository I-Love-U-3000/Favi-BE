using Favi_BE.API.Models.Entities;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IRepostRepository : IGenericRepository<Repost>
    {
        Task<Repost?> GetRepostAsync(Guid profileId, Guid originalPostId);
        Task<IEnumerable<Repost>> GetRepostsByProfileAsync(Guid profileId, int skip, int take);
        Task<int> GetRepostCountAsync(Guid postId);
        Task<bool> HasRepostedAsync(Guid profileId, Guid postId);
        Task<IEnumerable<Repost>> GetFeedRepostsAsync(Guid profileId, int skip, int take);
    }
}

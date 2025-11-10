using Favi_BE.Models.Entities.JoinTables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IFollowRepository : IGenericRepository<Follow>
    {
        Task<bool> IsFollowingAsync(Guid followerId, Guid followedId);
        Task<IEnumerable<Follow>> GetFollowersAsync(Guid profileId, int skip, int take);
        Task<IEnumerable<Follow>> GetFollowingAsync(Guid profileId, int skip, int take);
        Task<int> GetFollowersCountAsync(Guid profileId);
        Task<int> GetFollowingCountAsync(Guid profileId);
        Task<Follow?> GetAsync(Guid followerId, Guid followeeId);
    }
}
using Favi_BE.Models.Entities.JoinTables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IFollowRepository : IGenericRepository<Follow>
    {
        Task<bool> IsFollowingAsync(Guid followerId, Guid followedId);
        Task<IEnumerable<Follow>> GetFollowersAsync(Guid profileId, int skip, int take, string? query = null);
        Task<IEnumerable<Follow>> GetFollowingAsync(Guid profileId, int skip, int take, string? query = null);
        Task<int> GetFollowersCountAsync(Guid profileId, string? query = null);
        Task<int> GetFollowingCountAsync(Guid profileId, string? query = null);
        Task<Follow?> GetAsync(Guid followerId, Guid followeeId);
        Task<List<Guid>> GetFolloweeIdsAsync(Guid profileId, CancellationToken ct = default);
        Task<List<Guid>> GetFollowerIdsAsync(Guid profileId, CancellationToken ct = default);
    }
}
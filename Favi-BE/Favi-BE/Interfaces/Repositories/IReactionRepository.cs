using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IReactionRepository : IGenericRepository<Reaction>
    {
        Task<IEnumerable<Reaction>> GetReactionsByPostIdAsync(Guid postId);
        Task<Reaction> GetProfileReactionOnPostAsync(Guid profileId, Guid postId);
    }
}
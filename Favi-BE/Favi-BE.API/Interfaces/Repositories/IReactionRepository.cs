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
        Task<IEnumerable<Reaction>> GetReactionsByCommentIdAsync(Guid commentId);
        Task<IEnumerable<Reaction>> GetReactionsByCollectionIdAsync(Guid collectionId);
        Task<Reaction> GetProfileReactionOnPostAsync(Guid profileId, Guid postId);
        Task<Reaction> GetProfileReactionOnCommentAysnc(Guid profileId, Guid commentId);
        Task<Reaction> GetProfileReactionOnCollectionAsync(Guid profileId, Guid collectionId);
    }
}
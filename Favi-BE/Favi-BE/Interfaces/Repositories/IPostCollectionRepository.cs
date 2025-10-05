using Favi_BE.Models.Entities.JoinTables;
using System;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IPostCollectionRepository : IGenericRepository<PostCollection>
    {
        Task<bool> ExistsInCollectionAsync(Guid postId, Guid collectionId);
        Task RemoveFromCollectionAsync(Guid postId, Guid collectionId);
    }
}
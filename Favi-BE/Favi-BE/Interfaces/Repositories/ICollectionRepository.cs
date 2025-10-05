using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface ICollectionRepository : IGenericRepository<Collection>
    {
        Task<IEnumerable<Collection>> GetCollectionsByProfileIdAsync(Guid profileId);
        Task<Collection> GetCollectionWithPostsAsync(Guid collectionId);
    }
}
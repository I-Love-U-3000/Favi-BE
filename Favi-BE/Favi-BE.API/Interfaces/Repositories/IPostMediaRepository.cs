using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IPostMediaRepository : IGenericRepository<PostMedia>
    {
        Task<IEnumerable<PostMedia>> GetByPostIdAsync(Guid postId);

        Task<PostMedia?> GetProfileAvatar(Guid profileId);

        Task<PostMedia?> GetProfilePoster(Guid profileId);
    }
}
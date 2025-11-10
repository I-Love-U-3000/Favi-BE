using Favi_BE.Models.Entities;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IProfileRepository : IGenericRepository<Profile>
    {
        Task<Profile> GetByUsernameAsync(string username);
        Task<IEnumerable<Profile>> GetTopCreatorsAsync(int count);
        Task<bool> IsUsernameUniqueAsync(string username);
    }
}
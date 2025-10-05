using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface ISocialLinkRepository : IGenericRepository<SocialLink>
    {
        Task<IEnumerable<SocialLink>> GetByProfileIdAsync(Guid profileId);
    }
}
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class SocialLinkRepository : GenericRepository<SocialLink>, ISocialLinkRepository
    {
        public SocialLinkRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SocialLink>> GetByProfileIdAsync(Guid profileId)
        {
            return await _dbSet
                .Where(sl => sl.ProfileId == profileId)
                .ToListAsync();
        }
    }
}
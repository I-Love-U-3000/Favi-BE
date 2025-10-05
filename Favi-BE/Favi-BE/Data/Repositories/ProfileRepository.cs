using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class ProfileRepository : GenericRepository<Profile>, IProfileRepository
    {
        public ProfileRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Profile> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.Username.ToLower() == username.ToLower());
        }

        public async Task<IEnumerable<Profile>> GetTopCreatorsAsync(int count)
        {
            // Get profiles with most followers
            return await _context.Profiles
                .OrderByDescending(p => _context.Follows.Count(f => f.FolloweeId == p.Id))
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            return !await _dbSet.AnyAsync(p => p.Username.ToLower() == username.ToLower());
        }
    }
}
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class PostMediaRepository : GenericRepository<PostMedia>, IPostMediaRepository
    {
        public PostMediaRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PostMedia>> GetByPostIdAsync(Guid postId)
        {
            return await _dbSet
                .Where(pm => pm.PostId == postId)
                .OrderBy(pm => pm.Position)
                .ToListAsync();
        }
    }
}
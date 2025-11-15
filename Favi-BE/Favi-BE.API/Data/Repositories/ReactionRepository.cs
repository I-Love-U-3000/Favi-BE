using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class ReactionRepository : GenericRepository<Reaction>, IReactionRepository
    {
        public ReactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Reaction>> GetReactionsByPostIdAsync(Guid postId)
        {
            return await _dbSet
                .Where(r => r.PostId == postId)
                .Include(r => r.Profile)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reaction>> GetReactionsByCommentIdAsync(Guid commentId)
        {
            return await _dbSet
                .Where(r => r.CommentId == commentId)
                .Include(r => r.Profile)
                .ToListAsync();
        }

        public async Task<Reaction> GetProfileReactionOnPostAsync(Guid profileId, Guid postId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(r => r.ProfileId == profileId && r.PostId == postId);
        }

        public async Task<Reaction> GetProfileReactionOnCommentAysnc(Guid profileId, Guid commentId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(r => r.ProfileId == profileId && r.CommentId == commentId);
        }
    }
}
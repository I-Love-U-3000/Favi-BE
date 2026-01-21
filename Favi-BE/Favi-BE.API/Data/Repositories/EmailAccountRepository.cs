using Favi_BE.Data;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.Data.Repositories;

public class EmailAccountRepository : GenericRepository<EmailAccount>, IEmailAccountRepository
{
    public EmailAccountRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<EmailAccount?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(ea => ea.Profile)
            .FirstOrDefaultAsync(ea => ea.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        return !await _dbSet.AnyAsync(ea => ea.Email.ToLower() == email.ToLower());
    }
}

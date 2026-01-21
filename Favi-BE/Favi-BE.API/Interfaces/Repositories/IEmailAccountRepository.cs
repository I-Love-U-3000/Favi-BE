using Favi_BE.Models.Entities;

namespace Favi_BE.Interfaces.Repositories;

public interface IEmailAccountRepository : IGenericRepository<EmailAccount>
{
    Task<EmailAccount?> GetByEmailAsync(string email);
    Task<bool> IsEmailUniqueAsync(string email);
}

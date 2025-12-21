using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;

namespace Favi_BE.Data.Repositories
{
    public class AdminActionRepository : GenericRepository<AdminAction>, IAdminActionRepository
    {
        public AdminActionRepository(AppDbContext context) : base(context)
        {
        }
    }
}

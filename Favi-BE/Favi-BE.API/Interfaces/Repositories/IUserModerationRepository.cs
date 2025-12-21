using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IUserModerationRepository : IGenericRepository<UserModeration>
    {
        Task<UserModeration?> GetActiveModerationAsync(Guid profileId, ModerationActionType actionType);
        Task<IEnumerable<UserModeration>> GetByProfileAsync(Guid profileId, int skip, int take);
    }
}

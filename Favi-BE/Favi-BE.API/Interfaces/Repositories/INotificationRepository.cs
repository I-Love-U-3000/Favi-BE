using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.API.Interfaces.Repositories
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetNotificationsByRecipientIdAsync(Guid recipientId, int skip, int take);
        Task<int> GetUnreadCountAsync(Guid recipientId);
    }
}

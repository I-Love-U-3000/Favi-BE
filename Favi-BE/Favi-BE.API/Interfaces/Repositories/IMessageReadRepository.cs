using Favi_BE.API.Models.Entities.JoinTables;

namespace Favi_BE.API.Interfaces.Repositories
{
    public interface IMessageReadRepository
    {
        Task<MessageRead?> GetAsync(Guid messageId, Guid profileId);
        Task<IEnumerable<MessageRead>> GetReadsForMessageAsync(Guid messageId);
        Task MarkAsReadAsync(Guid messageId, Guid profileId);
        void Remove(MessageRead messageRead);
    }
}

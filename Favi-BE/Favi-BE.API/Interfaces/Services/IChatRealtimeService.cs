using Favi_BE.API.Models.Dtos;
using System.Threading.Tasks;

namespace Favi_BE.API.Interfaces.Services
{
    public interface IChatRealtimeService
    {
        Task NotifyConversationUpdatedAsync(Guid conversationId, object notification);
        Task NotifyMessageSentAsync(Guid conversationId, MessageDto message);
        Task NotifyUserJoinedAsync(Guid conversationId, Guid userId);
        Task NotifyUserLeftAsync(Guid conversationId, Guid userId);
        Task NotifyMessageReadAsync(Guid conversationId, Guid userId, Guid messageId);
    }
}
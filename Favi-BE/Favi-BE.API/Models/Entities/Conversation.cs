using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.API.Models.Enums;

namespace Favi_BE.API.Models.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public ConversationType Type { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? MutedUntil { get; set; }
        public DateTime? LastMessageAt { get; set; }

        // Navigation
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<UserConversation> UserConversations { get; set; } = new List<UserConversation>();
    }
}

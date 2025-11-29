using Favi_BE.Models.Entities;

namespace Favi_BE.API.Models.Entities.JoinTables
{
    public class UserConversation
    {
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = default!;

        public Guid ProfileId { get; set; }
        public Profile Profile { get; set; } = default!;

        public string Role { get; set; } = "member"; // owner, admin, member

        public Guid? LastReadMessageId { get; set; }
        public Message? LastReadMessage { get; set; }

        public DateTime JoinedAt { get; set; }
    }
}

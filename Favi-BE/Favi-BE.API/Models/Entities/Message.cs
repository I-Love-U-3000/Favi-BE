using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Models.Entities;

namespace Favi_BE.API.Models.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = default!;

        public Guid SenderId { get; set; }
        public Profile Sender { get; set; } = default!;

        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public Guid? PostId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }

        // Navigation property for message reads
        public ICollection<MessageRead> ReadBy { get; set; } = new List<MessageRead>();
    }
}

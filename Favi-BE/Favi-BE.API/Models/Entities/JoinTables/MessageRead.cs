using Favi_BE.Models.Entities;

namespace Favi_BE.API.Models.Entities.JoinTables
{
    public class MessageRead
    {
        public Guid MessageId { get; set; }
        public Message Message { get; set; } = default!;

        public Guid ProfileId { get; set; }
        public Profile Profile { get; set; } = default!;

        public DateTime ReadAt { get; set; }
    }
}

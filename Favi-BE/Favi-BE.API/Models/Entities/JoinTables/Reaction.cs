using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities.JoinTables
{
    public class Reaction
    {
        public Guid PostId { get; set; }
        public Guid ProfileId { get; set; }
        public ReactionType Type { get; set; }
        public DateTime CreatedAt { get; set; }

        public Post Post { get; set; } = null!;
        public Profile Profile { get; set; } = null!;
    }
}

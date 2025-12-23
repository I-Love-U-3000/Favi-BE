using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities.JoinTables
{
    //it shouldn't be contained in the join table folder, but moving it out causes complex refactoring
    public class Reaction
    {
        public Guid Id { get; set; }

        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid? CollectionId { get; set; }

        public Guid ProfileId { get; set; }
        public ReactionType Type { get; set; }
        public DateTime CreatedAt { get; set; }

        public Post? Post { get; set; }
        public Comment? Comment { get; set; }
        public Collection? Collection { get; set; }
        public Profile Profile { get; set; } = null!;
    }
}

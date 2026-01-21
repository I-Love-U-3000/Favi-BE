using Favi_BE.Models.Enums;
using Favi_BE.API.Models.Entities;

namespace Favi_BE.Models.Entities.JoinTables
{
    //it shouldn't be contained in the join table folder, but moving it out causes complex refactoring
    public class Reaction
    {
        public Guid Id { get; set; }

        public Guid? PostId { get; set; }
        public Guid? RepostId { get; set; }  // Can be null if reaction is on a Post or Comment
        public Guid? CommentId { get; set; }
        public Guid? CollectionId { get; set; }

        public Guid ProfileId { get; set; }
        public ReactionType Type { get; set; }
        public DateTime CreatedAt { get; set; }

        public Post? Post { get; set; }
        public Repost? Repost { get; set; }  // Optional: if reaction is on a Repost
        public Comment? Comment { get; set; }
        public Collection? Collection { get; set; }
        public Profile Profile { get; set; } = null!;
    }
}

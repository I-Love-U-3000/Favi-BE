using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;

namespace Favi_BE.API.Models.Entities
{
    public class Repost
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }  // User who is sharing the post
        public Guid OriginalPostId { get; set; }  // The post being shared
        public string? Caption { get; set; }  // Optional comment from the sharer
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Profile Profile { get; set; } = null!;
        public Post OriginalPost { get; set; } = null!;
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    }
}

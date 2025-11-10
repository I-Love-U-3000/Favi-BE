using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using System.Xml.Linq;

namespace Favi_BE.Models.Entities
{
    public class Post
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string? Caption { get; set; }
        public PrivacyLevel Privacy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Profile Profile { get; set; } = null!;
        public ICollection<PostMedia> PostMedias { get; set; } = new List<PostMedia>();
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
        public ICollection<PostCollection> PostCollections { get; set; } = new List<PostCollection>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    }
}

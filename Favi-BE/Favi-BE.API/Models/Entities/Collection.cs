using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities
{
    public class Collection
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? CoverImagePublicId { get; set; }
        public PrivacyLevel PrivacyLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Profile Profile { get; set; } = null!;
        public ICollection<PostCollection> PostCollections { get; set; } = new List<PostCollection>();
    }
}

using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities
{
    public class Story
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaPublicId { get; set; }
        public int MediaWidth { get; set; }
        public int MediaHeight { get; set; }
        public string? MediaFormat { get; set; }
        public string? ThumbnailUrl { get; set; }

        public PrivacyLevel Privacy { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public Profile Profile { get; set; } = null!;
        public ICollection<StoryView> StoryViews { get; set; } = new List<StoryView>();
    }
}

using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities
{
    public class SocialLink
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public SocialKind Kind { get; set; }
        public string Url { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Profile Profile { get; set; } = null!;
    }
}

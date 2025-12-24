using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public NotificationType Type { get; set; }
        public Guid RecipientProfileId { get; set; }
        public Guid ActorProfileId { get; set; }
        public Guid? TargetPostId { get; set; }
        public Guid? TargetCommentId { get; set; }
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Profile Recipient { get; set; } = null!;
        public Profile Actor { get; set; } = null!;
        public Post? TargetPost { get; set; }
        public Comment? TargetComment { get; set; }
    }
}

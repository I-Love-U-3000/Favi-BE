using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Microsoft.Extensions.Hosting;
using System.Xml.Linq;

namespace Favi_BE.Models.Entities
{
    public class Profile
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CoverUrl { get; set; }
        public string? Bio { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public PrivacyLevel PrivacyLevel { get; set; } = PrivacyLevel.Public;
        public PrivacyLevel FollowPrivacyLevel { get; set; } = PrivacyLevel.Public;
        public bool IsBanned { get; set; }
        public DateTime? BannedUntil { get; set; }

        // Navigation
        public ICollection<SocialLink> SocialLinks { get; set; } = new List<SocialLink>();
        public ICollection<Collection> Collections { get; set; } = new List<Collection>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<UserModeration> ModerationHistory { get; set; } = new List<UserModeration>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<UserConversation> UserConversations { get; set; } = new List<UserConversation>();
        public ICollection<Story> Stories { get; set; } = new List<Story>();
    }
}

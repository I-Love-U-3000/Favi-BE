using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<PostMedia> PostMedias { get; set; }
        public DbSet<SocialLink> SocialLinks { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<PostCollection> PostCollections { get; set; }
        public DbSet<PostTag> PostTags { get; set; }
        public DbSet<Reaction> Reactions { get; set; }
        public DbSet<UserModeration> UserModerations { get; set; }
        public DbSet<AdminAction> AdminActions { get; set; }
        public DbSet<Conversation> Conversations { get; set; } = default!;
        public DbSet<Message> Messages { get; set; } = default!;
        public DbSet<UserConversation> UserConversations { get; set; } = default!;
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<StoryView> StoryViews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Composite keys =====
            modelBuilder.Entity<PostTag>().HasKey(pt => new { pt.PostId, pt.TagId });
            modelBuilder.Entity<PostCollection>().HasKey(pc => new { pc.PostId, pc.CollectionId });
            modelBuilder.Entity<Follow>().HasKey(f => new { f.FollowerId, f.FolloweeId });
            modelBuilder.Entity<StoryView>().HasKey(sv => new { sv.StoryId, sv.ViewerProfileId });

            // ===== Post =====
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Profile)
                .WithMany(pf => pf.Posts)
                .HasForeignKey(p => p.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // PostMedia (Post -> PostMedias)
            modelBuilder.Entity<PostMedia>()
                .HasOne(m => m.Post)
                .WithMany(p => p.PostMedias)
                .HasForeignKey(m => m.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Comment =====
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Profile)
                .WithMany(pf => pf.Comments)
                .HasForeignKey(c => c.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // tránh cascade vòng khi xoá cha -> con
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(p => p.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== Collection =====
            modelBuilder.Entity<Collection>()
                .HasOne(c => c.Profile)
                .WithMany(pf => pf.Collections)
                .HasForeignKey(c => c.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // PostCollection
            modelBuilder.Entity<PostCollection>()
                .HasOne(pc => pc.Post)
                .WithMany(p => p.PostCollections)
                .HasForeignKey(pc => pc.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostCollection>()
                .HasOne(pc => pc.Collection)
                .WithMany(c => c.PostCollections)
                .HasForeignKey(pc => pc.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== PostTag =====
            modelBuilder.Entity<PostTag>()
                .HasOne(pt => pt.Post)
                .WithMany(p => p.PostTags)
                .HasForeignKey(pt => pt.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PostTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Reaction =====
            modelBuilder.Entity<Reaction>(entity =>
            {
                // Reaction -> Post (optional)
                entity.HasOne(r => r.Post)
                    .WithMany(p => p.Reactions)
                    .HasForeignKey(r => r.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Reaction -> Comment (optional)
                entity.HasOne(r => r.Comment)
                    .WithMany(c => c.Reactions)
                    .HasForeignKey(r => r.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Reaction -> Collection (optional)
                entity.HasOne(r => r.Collection)
                    .WithMany(col => col.Reactions)
                    .HasForeignKey(r => r.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Reaction -> Profile (required)
                entity.HasOne(r => r.Profile)
                    .WithMany(pf => pf.Reactions)
                    .HasForeignKey(r => r.ProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Mỗi profile chỉ được react 1 lần trên 1 post
                entity.HasIndex(r => new { r.PostId, r.ProfileId })
                    .IsUnique();

                // Mỗi profile chỉ được react 1 lần trên 1 comment
                entity.HasIndex(r => new { r.CommentId, r.ProfileId })
                    .IsUnique();

                // Mỗi profile chỉ được react 1 lần trên 1 collection
                entity.HasIndex(r => new { r.CollectionId, r.ProfileId })
                    .IsUnique();
            });

            // ===== Follow (self-ref Profile) =====
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Followee)
                .WithMany()
                .HasForeignKey(f => f.FolloweeId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== SocialLink =====
            modelBuilder.Entity<SocialLink>()
                .HasOne(sl => sl.Profile)
                .WithMany(pf => pf.SocialLinks)
                .HasForeignKey(sl => sl.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Report =====
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany(pf => pf.Reports)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== User Moderation =====
            modelBuilder.Entity<UserModeration>()
                .HasOne(um => um.Profile)
                .WithMany(p => p.ModerationHistory)
                .HasForeignKey(um => um.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserModeration>()
                .HasOne(um => um.Admin)
                .WithMany()
                .HasForeignKey(um => um.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserModeration>()
                .HasOne(um => um.AdminAction)
                .WithMany(a => a.UserModerations)
                .HasForeignKey(um => um.AdminActionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminAction>()
                .HasOne(a => a.Admin)
                .WithMany()
                .HasForeignKey(a => a.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // Conversation
            modelBuilder.Entity<Conversation>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Type)
                    .HasColumnType("integer");
                b.Property(c => c.CreatedAt)
                    .HasColumnType("timestamp with time zone");
                b.Property(c => c.MutedUntil)
                    .HasColumnType("timestamp with time zone");
                b.Property(c => c.LastMessageAt)
                    .HasColumnType("timestamp with time zone");
            });

            // Message
            modelBuilder.Entity<Message>(b =>
            {
                b.HasKey(m => m.Id);

                b.Property(m => m.CreatedAt)
                    .HasColumnType("timestamp with time zone");
                b.Property(m => m.UpdatedAt)
                    .HasColumnType("timestamp with time zone");

                b.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(m => m.Sender)
                    .WithMany(p => p.Messages) // cần thêm ICollection<Message> vào Profile
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserConversation (join)
            modelBuilder.Entity<UserConversation>(b =>
            {
                b.HasKey(uc => new { uc.ConversationId, uc.ProfileId });

                b.Property(uc => uc.JoinedAt)
                    .HasColumnType("timestamp with time zone");

                b.HasOne(uc => uc.Conversation)
                    .WithMany(c => c.UserConversations)
                    .HasForeignKey(uc => uc.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(uc => uc.Profile)
                    .WithMany(p => p.UserConversations) // cần thêm ICollection<UserConversation> vào Profile
                    .HasForeignKey(uc => uc.ProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(uc => uc.LastReadMessage)
                    .WithMany() // không cần navigation ngược
                    .HasForeignKey(uc => uc.LastReadMessageId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Notification
            modelBuilder.Entity<Notification>(b =>
            {
                b.HasKey(n => n.Id);

                b.Property(n => n.CreatedAt)
                    .HasColumnType("timestamp with time zone");

                b.Property(n => n.Type)
                    .HasColumnType("integer");

                // Notification -> Recipient
                b.HasOne(n => n.Recipient)
                    .WithMany()
                    .HasForeignKey(n => n.RecipientProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Notification -> Actor
                b.HasOne(n => n.Actor)
                    .WithMany()
                    .HasForeignKey(n => n.ActorProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Notification -> Post (optional)
                b.HasOne(n => n.TargetPost)
                    .WithMany()
                    .HasForeignKey(n => n.TargetPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Notification -> Comment (optional)
                b.HasOne(n => n.TargetComment)
                    .WithMany()
                    .HasForeignKey(n => n.TargetCommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index for faster queries
                b.HasIndex(n => new { n.RecipientProfileId, n.CreatedAt });
            });

            // ===== Story =====
            modelBuilder.Entity<Story>(b =>
            {
                b.HasKey(s => s.Id);

                b.Property(s => s.CreatedAt)
                    .HasColumnType("timestamp with time zone");
                b.Property(s => s.ExpiresAt)
                    .HasColumnType("timestamp with time zone");

                b.HasIndex(s => s.ExpiresAt); // For expiring stories query

                b.HasOne(s => s.Profile)
                    .WithMany(p => p.Stories)
                    .HasForeignKey(s => s.ProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== StoryView =====
            modelBuilder.Entity<StoryView>(b =>
            {
                b.Property(sv => sv.ViewedAt)
                    .HasColumnType("timestamp with time zone");

                b.HasOne(sv => sv.Story)
                    .WithMany(s => s.StoryViews)
                    .HasForeignKey(sv => sv.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(sv => sv.Viewer)
                    .WithMany()
                    .HasForeignKey(sv => sv.ViewerProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

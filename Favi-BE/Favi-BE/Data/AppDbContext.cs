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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Composite keys =====
            modelBuilder.Entity<PostTag>().HasKey(pt => new { pt.PostId, pt.TagId });
            modelBuilder.Entity<PostCollection>().HasKey(pc => new { pc.PostId, pc.CollectionId });
            modelBuilder.Entity<Follow>().HasKey(f => new { f.FollowerId, f.FolloweeId });
            modelBuilder.Entity<Reaction>().HasKey(r => new { r.PostId, r.ProfileId });

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
            modelBuilder.Entity<Reaction>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reaction>()
                .HasOne(r => r.Profile)
                .WithMany(pf => pf.Reactions)
                .HasForeignKey(r => r.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

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
        }

    }
}

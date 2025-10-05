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

            // Fluent API config cho composite keys
            modelBuilder.Entity<PostTag>().HasKey(pt => new { pt.PostId, pt.TagId });
            modelBuilder.Entity<PostCollection>().HasKey(pc => new { pc.PostId, pc.CollectionId });
            modelBuilder.Entity<Follow>().HasKey(f => new { f.FollowerId, f.FolloweeId });
            modelBuilder.Entity<Reaction>().HasKey(r => new { r.PostId, r.ProfileId });
        }
    }
}

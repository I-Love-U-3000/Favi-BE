namespace Favi_BE.Models.Entities
{
    public class PostMedia
    {
        public Guid Id { get; set; }
        public Guid? PostId { get; set; }
        public Guid? ProfileId { get; set; }
        public string Url { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public int Position { get; set; } 

        public Post? Post { get; set; }

        public string PublicId { get; set; } = null!; 
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = null!;

        public bool IsAvatar { get; set; } = false;
        public bool IsPoster { get; set; } = false;
    }
}

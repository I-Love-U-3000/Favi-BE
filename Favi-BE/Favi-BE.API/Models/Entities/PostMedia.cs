namespace Favi_BE.Models.Entities
{
    public class PostMedia
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public string Url { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public int Position { get; set; } // thứ tự hiển thị trong post

        public Post Post { get; set; } = null!;

        public string PublicId { get; set; } = null!; //currently used for Cloudinary
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = null!;

    }

}

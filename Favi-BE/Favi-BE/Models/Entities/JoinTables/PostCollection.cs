namespace Favi_BE.Models.Entities.JoinTables
{
    public class PostCollection
    {
        public Guid PostId { get; set; }
        public Guid CollectionId { get; set; }

        public Post Post { get; set; } = null!;
        public Collection Collection { get; set; } = null!;
    }
}

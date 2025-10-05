using Favi_BE.Models.Entities.JoinTables;

namespace Favi_BE.Models.Entities
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    }
}

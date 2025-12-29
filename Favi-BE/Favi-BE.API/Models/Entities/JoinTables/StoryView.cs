namespace Favi_BE.Models.Entities.JoinTables
{
    public class StoryView
    {
        public Guid StoryId { get; set; }
        public Guid ViewerProfileId { get; set; }
        public DateTime ViewedAt { get; set; }

        public Story Story { get; set; } = null!;
        public Profile Viewer { get; set; } = null!;
    }
}

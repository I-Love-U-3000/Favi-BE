    namespace Favi_BE.Models.Entities.JoinTables
{
    public class Follow
    {
        public Guid FollowerId { get; set; }
        public Guid FolloweeId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Profile Follower { get; set; } = null!;
        public Profile Followee { get; set; } = null!;
    }
}

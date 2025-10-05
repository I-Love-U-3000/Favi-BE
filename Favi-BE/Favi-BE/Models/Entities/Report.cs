using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities
{
    public class Report
    {
        public Guid Id { get; set; }
        public Guid ReporterId { get; set; }
        public ReportTarget TargetType { get; set; }
        public Guid TargetId { get; set; }
        public string? Reason { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public DateTime CreatedAt { get; set; }
        public DateTime? ActedAt { get; set; }
        public string? Data { get; set; }  // JSON

        public Profile? Reporter { get; set; }
    }
}

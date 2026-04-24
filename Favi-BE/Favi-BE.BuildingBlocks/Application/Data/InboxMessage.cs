namespace Favi_BE.BuildingBlocks.Application.Data;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public DateTime ReceivedOnUtc { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public InboxMessageStatus Status { get; set; } = InboxMessageStatus.Processing;
    public int Retries { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}

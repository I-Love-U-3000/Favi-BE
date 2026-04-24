namespace Favi_BE.BuildingBlocks.Application.Inbox;

public interface IInbox
{
    Task<bool> TryStartProcessingAsync(string messageId, string consumer, string type, string payload, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(string messageId, string consumer, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string messageId, string consumer, string error, CancellationToken cancellationToken = default);
}

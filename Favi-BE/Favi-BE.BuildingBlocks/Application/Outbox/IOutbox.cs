namespace Favi_BE.BuildingBlocks.Application.Outbox;

public interface IOutbox
{
    Task AddAsync(IEnumerable<OutboxMessageData> messages, CancellationToken cancellationToken = default);
}

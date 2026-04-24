namespace Favi_BE.BuildingBlocks.Application.Inbox;

public interface IInboxConsumer
{
    string MessageType { get; }
    Task HandleAsync(string payload, CancellationToken cancellationToken = default);
}

using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Application.Events;

public interface IDomainEventsDispatcher
{
    Task DispatchEventsAsync(string? correlationId = null, string? causationId = null, CancellationToken cancellationToken = default);
}

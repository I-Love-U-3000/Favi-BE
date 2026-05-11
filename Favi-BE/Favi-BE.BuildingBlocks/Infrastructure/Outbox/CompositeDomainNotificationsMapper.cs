using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Outbox;
using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Composite IDomainNotificationsMapper: tries each registered IModuleDomainEventMapper in order.
/// Falls back to serializing the domain event as-is when no module mapper handles it.
/// Module mappers may return null to suppress an outbox entry (e.g. self-notification guard).
/// </summary>
public sealed class CompositeDomainNotificationsMapper : IDomainNotificationsMapper
{
    private readonly IEnumerable<IModuleDomainEventMapper> _moduleMappers;

    public CompositeDomainNotificationsMapper(IEnumerable<IModuleDomainEventMapper> moduleMappers)
    {
        _moduleMappers = moduleMappers;
    }

    public async Task<OutboxMessageData?> MapAsync(IDomainEvent domainEvent, string? correlationId = null, string? causationId = null)
    {
        foreach (var mapper in _moduleMappers)
        {
            if (mapper.CanHandle(domainEvent))
                return await mapper.MapAsync(domainEvent, correlationId, causationId);
        }

        return new OutboxMessageData(
            Id: Guid.NewGuid(),
            OccurredOnUtc: domainEvent.OccurredOnUtc,
            Type: domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            Payload: JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            CorrelationId: correlationId,
            CausationId: causationId);
    }
}

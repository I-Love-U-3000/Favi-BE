using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Outbox;
using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Infrastructure.Outbox;

public sealed class DomainNotificationsMapper : IDomainNotificationsMapper
{
    public OutboxMessageData Map(IDomainEvent domainEvent, string? correlationId = null, string? causationId = null)
    {
        return new OutboxMessageData(
            Id: Guid.NewGuid(),
            OccurredOnUtc: domainEvent.OccurredOnUtc,
            Type: domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            Payload: JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            CorrelationId: correlationId,
            CausationId: causationId);
    }
}

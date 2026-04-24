using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.BuildingBlocks.Application.Outbox;

public interface IDomainNotificationsMapper
{
    OutboxMessageData Map(IDomainEvent domainEvent, string? correlationId = null, string? causationId = null);
}

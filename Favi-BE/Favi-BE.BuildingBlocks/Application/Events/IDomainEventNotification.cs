using Favi_BE.BuildingBlocks.Domain;
using MediatR;

namespace Favi_BE.BuildingBlocks.Application.Events;

public interface IDomainEventNotification<out TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    TDomainEvent DomainEvent { get; }
}

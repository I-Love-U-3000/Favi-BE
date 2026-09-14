using MediatR;

namespace Favi_BE.BuildingBlocks.Domain;

public interface IDomainEvent : INotification
{
    DateTime OccurredOnUtc { get; }
}

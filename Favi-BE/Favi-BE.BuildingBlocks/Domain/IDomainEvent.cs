namespace Favi_BE.BuildingBlocks.Domain;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}

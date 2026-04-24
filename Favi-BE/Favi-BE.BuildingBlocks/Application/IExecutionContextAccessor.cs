namespace Favi_BE.BuildingBlocks.Application;

public interface IExecutionContextAccessor
{
    Guid? UserId { get; }
    string? CorrelationId { get; }
}

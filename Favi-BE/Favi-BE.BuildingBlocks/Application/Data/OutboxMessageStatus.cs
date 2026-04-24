namespace Favi_BE.BuildingBlocks.Application.Data;

public enum OutboxMessageStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2,
    Poisoned = 3
}

namespace Favi_BE.BuildingBlocks.Application.Outbox;

public sealed record OutboxMessageData(
    Guid Id,
    DateTime OccurredOnUtc,
    string Type,
    string Payload,
    string? CorrelationId,
    string? CausationId);

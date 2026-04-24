# Outbox Implementation Guide

## 1. Scope
Bảo đảm publish integration events reliable và atomic với state changes.

## 2. Data model
Use `OutboxMessages` table (see schema plan).

## 3. Write flow
1. Command handler mutates aggregate.
2. Aggregate raises domain events.
3. Transaction behavior calls `SaveChanges`.
4. Domain events mapped to integration notifications.
5. Outbox rows persisted in same transaction.
6. Commit success -> processor picks pending outbox.

## 4. Components
- `IDomainEventsAccessor`
- `IDomainNotificationsMapper`
- `IOutbox` abstraction
- `OutboxProcessor` hosted service

## 5. Processor behavior
- Pull by `Status = Pending` ordered by `OccurredOnUtc`.
- Publish to integration pipeline.
- On success: `Status = Processed`, set `ProcessedOnUtc`.
- On failure: increment `Retries`, capture `Error`.
- On max retry: `Status = Poisoned` + alert.

## 6. Reliability rules
- Idempotent consumer downstream required.
- Correlation/causation IDs propagated.
- No SignalR push in transaction path.

## 7. Observability
- Metrics: pending count, processed/sec, retry count, poison count.
- Logs: structured with `MessageId`, `EventType`, `CorrelationId`.
- Traces: span for outbox dequeue/publish.
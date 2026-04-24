# BuildingBlocks Design

## 1. Package layout
- `BuildingBlocks.Domain`
- `BuildingBlocks.Application`
- `BuildingBlocks.Infrastructure`

## 2. Domain layer contracts
| Contract | Purpose |
|---|---|
| `Entity` | base aggregate/entity with domain events collection |
| `ValueObject` | equality by components |
| `IBusinessRule` | domain invariant abstraction |
| `IDomainEvent` | marker for domain events |
| `TypedIdValueBase` | strongly typed id base |
| `BusinessRuleValidationException` | rule violation exception |

### 2.1 Domain event policy
- Aggregate adds domain event during state transition.
- Domain events cleared only after successful outbox enqueue.

## 3. Application layer contracts
| Contract | Purpose |
|---|---|
| `IExecutionContextAccessor` | user/correlation context access |
| `IDomainEventNotification<T>` | adapter from domain event -> MediatR notification |
| `IOutbox` | append outbox messages in transaction |
| `IInbox` | idempotent consume contracts |
| `ICommand` / `IQuery` (optional abstractions) | semantic separation for handlers |

## 4. Infrastructure layer components
- `DomainEventsAccessor` (EF ChangeTracker scan).
- `DomainEventsDispatcher` (in-process publish).
- `OutboxMessage` entity + repository.
- `InboxMessage` entity + repository.
- `OutboxProcessor` hosted service (retry, poison handling).
- `InboxProcessor`/consumer pipeline.

## 5. Pipeline behaviors (order)
1. Validation
2. Logging
3. Performance
4. Transaction (commands only)

## 6. Rules
- No infrastructure dependency from domain.
- Application depends only on abstractions.
- Infrastructure implements abstractions and wires DI.
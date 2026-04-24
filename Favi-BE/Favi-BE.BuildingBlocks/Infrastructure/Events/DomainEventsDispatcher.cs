using System.Linq;
using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.BuildingBlocks.Application.Outbox;
using MediatR;

namespace Favi_BE.BuildingBlocks.Infrastructure.Events;

public sealed class DomainEventsDispatcher : IDomainEventsDispatcher
{
    private readonly IMediator _mediator;
    private readonly IDomainEventsAccessor _domainEventsAccessor;
    private readonly IDomainNotificationsMapper _domainNotificationsMapper;
    private readonly IOutbox _outbox;

    public DomainEventsDispatcher(
        IMediator mediator,
        IDomainEventsAccessor domainEventsAccessor,
        IDomainNotificationsMapper domainNotificationsMapper,
        IOutbox outbox)
    {
        _mediator = mediator;
        _domainEventsAccessor = domainEventsAccessor;
        _domainNotificationsMapper = domainNotificationsMapper;
        _outbox = outbox;
    }

    public async Task DispatchEventsAsync(string? correlationId = null, string? causationId = null, CancellationToken cancellationToken = default)
    {
        var domainEvents = _domainEventsAccessor.GetAllDomainEvents();

        if (domainEvents.Count == 0)
        {
            return;
        }

        // Phase A: In-process dispatching (MediatR Publish)
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        // Phase B: Cross-boundary dispatching (Outbox enqueue)
        var outboxMessages = domainEvents
            .Select(x => _domainNotificationsMapper.Map(x, correlationId, causationId))
            .ToList();

        await _outbox.AddAsync(outboxMessages, cancellationToken);

        // Clear events after dispatching to avoid duplicates
        _domainEventsAccessor.ClearAllDomainEvents();
    }
}

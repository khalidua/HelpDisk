using HelpDisk.Application.Abstractions.Events;
using HelpDisk.Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDisk.Infrastructure.Services;

/// <summary>
/// Finds the registered handlers for each domain event and runs them.
/// </summary>
/// <remarks>
/// ============================================================================
/// THIS IS MediatR's PUBLISH, IN ABOUT THIRTY LINES YOU CAN READ.
/// ============================================================================
///
/// The problem it solves: we have a List&lt;IDomainEvent&gt; whose real types are
/// only known at runtime, and handlers are registered as
/// IDomainEventHandler&lt;TicketAssignedDomainEvent&gt; - a different closed generic
/// per event type. There is no compile-time way to ask the container for "the
/// handlers for this object", so we build the type at runtime.
///
/// Line by line:
///
///   1. domainEvent.GetType()            -> TicketAssignedDomainEvent
///   2. MakeGenericType(...)             -> IDomainEventHandler&lt;TicketAssignedDomainEvent&gt;
///   3. GetServices(handlerType)         -> every handler registered for it
///   4. Invoke HandleAsync on each
///
/// Reflection is the price of dynamic dispatch. If you have used MediatR, this
/// is what it was doing for you - and knowing that makes the library much less
/// mysterious.
///
/// Handlers run SEQUENTIALLY and in registration order. Running them in
/// parallel with Task.WhenAll looks tempting and is a trap: they resolve from
/// the same scope and would share one DbContext, which is not thread-safe.
/// </remarks>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            // Build IDomainEventHandler<TConcreteEvent> for this event's actual
            // runtime type.
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            // GetServices (plural) - an event may have any number of handlers,
            // including none. Zero handlers is a perfectly valid outcome: the
            // fact still happened, nobody currently cares.
            var handlers = _serviceProvider.GetServices(handlerType);

            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
                ?? throw new InvalidOperationException(
                    $"Could not find HandleAsync on {handlerType.Name}.");

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var task = (Task?)handleMethod.Invoke(handler, [domainEvent, cancellationToken]);

                if (task is not null)
                {
                    await task;
                }
            }
        }
    }
}

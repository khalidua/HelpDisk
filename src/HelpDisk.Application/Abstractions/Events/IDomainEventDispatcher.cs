using HelpDisk.Domain.Primitives;

namespace HelpDisk.Application.Abstractions.Events;

/// <summary>
/// Finds and runs the handlers for a batch of domain events.
/// </summary>
/// <remarks>
/// Called by DomainEventsInterceptor after SaveChangesAsync succeeds. The
/// ordering matters: events are dispatched only once the transaction has
/// committed, so a handler never announces something that was rolled back.
///
/// The flip side, worth being explicit about with students: handlers run AFTER
/// the commit, so a handler that fails cannot undo it. If TicketAssigned's
/// e-mail fails to send, the ticket stays assigned. That is usually what you
/// want - but if it is not, the answer is an outbox table, not a bigger
/// transaction. Out of scope here; worth knowing the name.
/// </remarks>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}

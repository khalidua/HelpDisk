using HelpDisk.Application.Abstractions.Events;
using HelpDisk.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HelpDisk.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Publishes domain events after a successful save.
/// </summary>
/// <remarks>
/// Ticket.Assign raises TicketAssignedDomainEvent, which does nothing but sit
/// in a list on the entity. This interceptor is what finally makes something
/// happen.
///
/// ---------------------------------------------------------------------------
/// THE TIMING IS THE WHOLE DESIGN. Note the method being overridden:
///
///     SavingChangesAsync   runs BEFORE the SQL      <- the other interceptors
///     SavedChangesAsync    runs AFTER it succeeded  <- this one
///
/// Events are dispatched after the write has committed. If SaveChanges throws -
/// a constraint violation, a deadlock, a dropped connection - this method never
/// runs and no event fires. You will never send "your ticket was assigned to
/// Sam" for an assignment that got rolled back.
///
/// The honest limitation, worth saying out loud to students: the commit has
/// already happened when handlers run, so a failing handler cannot undo it. If
/// the e-mail send throws, the ticket stays assigned and the notification is
/// simply lost. For most reactions that is the correct trade. When it is not,
/// the answer is the OUTBOX PATTERN - write the event to a table inside the
/// same transaction, and have a background worker deliver it with retries.
/// Out of scope for this template, but that is the term to search for.
/// ---------------------------------------------------------------------------
/// </remarks>
public sealed class DomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventsInterceptor(IDomainEventDispatcher dispatcher) => _dispatcher = dispatcher;

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PublishDomainEventsAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task PublishDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        // IHasDomainEvents rather than AggregateRoot<T> - see that interface for
        // why the generic base class cannot be queried here.
        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        // Clear BEFORE dispatching, not after. The entities stay tracked for
        // the rest of the request, so if anything calls SaveChanges again these
        // same events would be collected and published a second time. Clearing
        // first also means a handler that throws does not leave the events
        // queued to fire again on the next save.
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        if (domainEvents.Count == 0)
        {
            return;
        }

        await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}

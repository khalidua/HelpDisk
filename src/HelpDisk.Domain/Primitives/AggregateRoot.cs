namespace HelpDisk.Domain.Primitives;

/// <summary>
/// An entity that is the entry point to a cluster of objects treated as one
/// unit for data changes.
/// </summary>
/// <remarks>
/// AGGREGATE ROOT is the DDD idea people most often skip, and skipping it is
/// why "we do DDD" so often means "we have a folder called Domain".
///
/// An aggregate is a consistency boundary. The rules:
///
///   1. Outside code may only hold a reference to the ROOT, never to an entity
///      inside it. Nothing outside Ticket may hold a TicketComment.
///   2. Anything inside the aggregate is changed by calling a method ON the
///      root. That is why TicketComment has an internal constructor and is only
///      created by Ticket.AddComment.
///   3. One transaction changes one aggregate. If you find yourself needing to
///      change two aggregates atomically, that is usually a sign the boundary
///      is drawn in the wrong place.
///   4. Aggregates reference each other BY ID, never by object reference. Look
///      at Ticket.CategoryId - there is deliberately no "Category Category"
///      navigation property, even though EF would happily give you one.
///
/// In this template, Ticket is an aggregate root and TicketComment is not.
/// Category is neither - it is a plain Entity, because it has no invariants
/// worth defending and no children. Not every table needs DDD ceremony, and
/// pretending otherwise is how people conclude DDD is bureaucracy.
/// </remarks>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IHasDomainEvents
    where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TKey id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Events raised but not yet published. Read by DomainEventsInterceptor
    /// after the transaction commits.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records that something happened, WITHOUT doing anything about it.
    /// </summary>
    /// <remarks>
    /// This is the trick that keeps Domain framework-free. Ticket.Assign wants
    /// to notify the new assignee, but Domain must not know what e-mail is. So
    /// it raises a fact and lets an outer layer decide what a fact means.
    ///
    /// Note that raising is not publishing. Nothing is dispatched until
    /// SaveChangesAsync succeeds - so an event never claims a ticket was
    /// assigned if the transaction rolled back.
    /// </remarks>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Called by DomainEventsInterceptor once events have been dispatched, so
    /// they are not published twice if the entity is saved again.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

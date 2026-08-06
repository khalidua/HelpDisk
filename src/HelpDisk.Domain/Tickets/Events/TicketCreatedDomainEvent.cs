using HelpDisk.Domain.Primitives;

namespace HelpDisk.Domain.Tickets.Events;

/// <summary>
/// Raised when a ticket is first created.
/// </summary>
/// <remarks>
/// Carries IDs, not the Ticket object. That is deliberate: by the time a
/// handler runs, the aggregate may have moved on, and a handler holding a live
/// entity is tempted to modify it - which would mean two aggregates changing in
/// one transaction. Pass identity and let the handler load what it needs.
/// </remarks>
public sealed record TicketCreatedDomainEvent(Guid TicketId, string ReporterId) : IDomainEvent;

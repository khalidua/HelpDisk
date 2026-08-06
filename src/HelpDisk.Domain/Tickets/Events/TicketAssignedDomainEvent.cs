using HelpDisk.Domain.Primitives;

namespace HelpDisk.Domain.Tickets.Events;

/// <summary>
/// Raised when a ticket is assigned to somebody.
/// </summary>
/// <remarks>
/// This is the event that shows why domain events exist at all.
///
/// The business rule is "when a ticket is assigned, tell the assignee". Without
/// domain events you would put that in TicketService.AssignAsync, and
/// TicketService would need IEmailService, and soon a Slack client, and a
/// metrics counter - a method about assignment slowly becoming a method about
/// notification plumbing.
///
/// Instead Ticket.Assign states the fact and stops. Handlers subscribe. Adding
/// a Slack notification later means adding one new class and changing nothing
/// that already works.
/// </remarks>
public sealed record TicketAssignedDomainEvent(Guid TicketId, string AssigneeId) : IDomainEvent;

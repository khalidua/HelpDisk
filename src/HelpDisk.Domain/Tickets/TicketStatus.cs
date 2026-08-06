namespace HelpDisk.Domain.Tickets;

/// <summary>
/// Where a ticket sits in its lifecycle.
/// </summary>
/// <remarks>
/// The legal transitions, enforced by Ticket's methods and nowhere else:
///
///     New ──Assign──> InProgress ──Close──> Closed
///      │                  │                    │
///      └──────Close───────┘                    │
///                                              │
///     New / InProgress <────────Reopen─────────┘
///
/// Explicit integer values are assigned so the numbers stored in the database
/// never shift. Insert a new member in the middle without a value and every
/// existing row silently changes meaning - a genuinely nasty production bug,
/// because nothing fails, the data just becomes wrong.
/// </remarks>
public enum TicketStatus
{
    New = 1,
    InProgress = 2,
    Closed = 3
}

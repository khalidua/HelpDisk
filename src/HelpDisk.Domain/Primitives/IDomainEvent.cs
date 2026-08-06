namespace HelpDisk.Domain.Primitives;

/// <summary>
/// Marks a record as something interesting that HAPPENED in the domain.
/// </summary>
/// <remarks>
/// Domain events are always named in the past tense - TicketAssignedDomainEvent,
/// not AssignTicketCommand. That is not a style rule, it is a semantic one: an
/// event is a fact that has already occurred and cannot be rejected. A handler
/// may react to it, but it cannot veto it.
///
/// This is an empty marker interface on purpose. Each event record carries only
/// the data it needs. Resist the urge to add EventId/OccurredOnUtc here "just in
/// case" - nothing in this template reads them, and unused ceremony is the thing
/// that makes patterns look pointless to people learning them.
/// </remarks>
public interface IDomainEvent
{
}

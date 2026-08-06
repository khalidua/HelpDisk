namespace HelpDisk.Domain.Primitives;

/// <summary>
/// An entity that is never really deleted, only hidden.
/// </summary>
/// <remarks>
/// Support tickets are a good example of data you must not physically delete:
/// you need the audit trail, and somebody will eventually ask "what happened to
/// ticket #4471?".
///
/// Two pieces of Infrastructure make this work, and neither is visible from
/// Application:
///
///   1. SoftDeleteInterceptor turns a DELETE into an UPDATE that sets
///      IsDeleted = true.
///   2. A global query filter in AppDbContext appends "WHERE IsDeleted = 0" to
///      every query automatically.
///
/// So TicketService writes _tickets.Remove(ticket) and reads back a list that
/// no longer contains it - exactly as if the row were gone. The service never
/// learns that soft delete exists. That is the point.
///
/// The trap worth teaching: global query filters apply to navigation properties
/// too, and IgnoreQueryFilters() is the escape hatch when an admin screen
/// genuinely needs to see deleted rows.
/// </remarks>
public interface ISoftDeleteEntity
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAtUtc { get; set; }

    DateTime? RestoredAtUtc { get; set; }
}

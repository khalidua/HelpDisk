namespace HelpDisk.Domain.Primitives;

/// <summary>
/// Something that can accumulate domain events. Implemented by
/// <see cref="AggregateRoot{TKey}"/>.
/// </summary>
/// <remarks>
/// WHY THIS EXISTS - a small but genuinely useful C# lesson.
///
/// DomainEventsInterceptor needs to sweep EF's change tracker for every entity
/// carrying unpublished events. The obvious way does not work:
///
///     context.ChangeTracker.Entries&lt;AggregateRoot&lt;TKey&gt;&gt;()   // what is TKey here?
///
/// AggregateRoot is generic, so there is no single type to ask for. You could
/// write:
///
///     context.ChangeTracker.Entries().Where(e =&gt; e.Entity is AggregateRoot&lt;Guid&gt;)
///
/// but that silently ignores any aggregate keyed by int, long or string. It
/// would work today - every aggregate here uses Guid - and break the day
/// somebody adds one that does not, with no error, just events that quietly
/// never fire.
///
/// A non-generic interface fixes it: Entries&lt;IHasDomainEvents&gt;() catches every
/// aggregate regardless of key type.
///
/// THE GENERAL RULE: use a generic base class to SHARE IMPLEMENTATION, and a
/// non-generic interface to QUERY ACROSS A SET. When you need both, do both -
/// they are not competing choices.
/// </remarks>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

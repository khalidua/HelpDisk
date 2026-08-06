namespace HelpDisk.Domain.Primitives;

/// <summary>
/// Base class for anything with an identity that persists over time.
/// </summary>
/// <remarks>
/// The defining trait of an entity is that it is identified by its Id, not by
/// its values. Two tickets with identical titles are still two different
/// tickets. Rename a ticket and it is still the same ticket.
///
/// Contrast with a value object (not used in this template, but worth knowing):
/// a Money of 50 USD is interchangeable with any other Money of 50 USD, so it
/// has no Id and equality compares its values instead.
/// </remarks>
/// <typeparam name="TKey">
/// The identity type. This template uses Guid so that a new entity has its Id
/// the instant it is constructed, before the database has seen it - which is
/// what lets Ticket.Create raise a domain event carrying its own Id. With a
/// database-generated int you would not know the Id until after SaveChanges.
/// </typeparam>
public abstract class Entity<TKey> : IAuditableEntity
    where TKey : notnull
{
    protected Entity(TKey id) => Id = id;

    /// <summary>
    /// Required by EF Core, which materialises entities without going through
    /// your real constructor. Keep it protected so application code cannot
    /// build a half-initialised entity by accident.
    /// </summary>
    protected Entity()
    {
    }

    public TKey Id { get; protected set; } = default!;

    // Stamped by AuditableEntityInterceptor. See IAuditableEntity.
    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }
}

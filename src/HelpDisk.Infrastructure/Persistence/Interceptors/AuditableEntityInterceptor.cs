using HelpDisk.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HelpDisk.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps CreatedOnUtc and ModifiedOnUtc on every save.
/// </summary>
/// <remarks>
/// An EF Core interceptor is a hook into the DbContext lifecycle. This one runs
/// just before the SQL is sent, walks everything EF is about to write, and sets
/// the audit fields.
///
/// WHY THIS IS BETTER THAN DOING IT IN THE SERVICE: it cannot be forgotten. Not
/// by the service, not by the seeder, not by the developer who joins next year
/// and writes a bulk import. There is exactly one code path to the database and
/// this sits on it.
///
/// This is what "cross-cutting concern" means concretely: behaviour that is
/// true of everything, so it should be written once at a choke point, not
/// repeated at every call site and eventually missed at one.
/// </remarks>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Both the sync and async entry points call this, because EF will use
    /// whichever the caller invoked. Overriding only the async one is a common
    /// slip: everything works until somebody calls SaveChanges() without await
    /// and the audit fields silently stay at their default values.
    /// </summary>
    private static void StampAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;

        // Entries<IAuditableEntity>() finds every tracked entity implementing
        // the interface, whatever its concrete type - the same non-generic-
        // interface trick described in IHasDomainEvents.
        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOnUtc = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedOnUtc = utcNow;
            }
        }
    }
}

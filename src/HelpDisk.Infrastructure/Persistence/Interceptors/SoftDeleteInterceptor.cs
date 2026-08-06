using HelpDisk.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HelpDisk.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Turns deletes into updates for anything implementing
/// <see cref="ISoftDeleteEntity"/>.
/// </summary>
/// <remarks>
/// TicketService writes _tickets.Remove(ticket) and means it. This interceptor
/// quietly rewrites that intent: EF is about to emit DELETE FROM Tickets, and
/// we change the entity's state to Modified with IsDeleted = true, so it emits
/// an UPDATE instead.
///
/// Paired with the global query filter in TicketConfiguration
/// (HasQueryFilter(t => !t.IsDeleted)), the row becomes invisible to every
/// subsequent query. From Application's point of view the ticket is gone.
///
/// Together those two pieces mean soft delete is implemented ONCE, in
/// Infrastructure, and no service method ever mentions it. Compare with the
/// usual approach - every query remembering ".Where(x => !x.IsDeleted)" - where
/// the feature works until the one query that forgets.
///
/// ---------------------------------------------------------------------------
/// ORDER MATTERS. This interceptor must be registered BEFORE
/// AuditableEntityInterceptor (see DependencyInjection). EF runs them in
/// registration order, and this one flips the state from Deleted to Modified -
/// which is what lets the auditable interceptor then see a Modified entity and
/// stamp ModifiedOnUtc. Reverse the order and a soft-deleted row keeps a stale
/// modification date.
/// ---------------------------------------------------------------------------
/// </remarks>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ConvertDeletesToUpdates(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ConvertDeletesToUpdates(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertDeletesToUpdates(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeleteEntity>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            // The line that does the work: EF now believes this is an update.
            entry.State = EntityState.Modified;

            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = DateTime.UtcNow;
            entry.Entity.RestoredAtUtc = null;
        }
    }
}
